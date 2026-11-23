using System;
using System.Collections.Generic;
using System.Threading;
using Game.Building;
using Game.Core;
using Game.Inventory;
using Game.Player;
using Game.Save;
using Game.Survival;
using Game.UI;
using Game.World;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Maps
{
    /// <summary>
    /// 맵 씬 전환의 유일한 창구. GameServices에 등록되어 문·맵 선택에서 호출한다 (ARCHITECTURE.md 3.3).
    /// Boot 씬에 하나 두고, Awake에서 자기 참조만 캐시한 뒤 Start에서 시작 맵으로 이동한다. 맵 이동이
    /// 끝날 때마다 자동 저장한다 (docs/design/GAME_DESIGN.md 9).
    /// </summary>
    public sealed class MapLoader : MonoBehaviour
    {
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private CinemachineConfiner2D cameraConfiner;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerSurvival playerSurvival;
        [SerializeField] private PlayerStatusEffects playerStatusEffects;
        [SerializeField] private MapDefinition startMap;
        [SerializeField] private string startSpawnPointId = "";

        private IWorldClock worldClock;
        private MapStateRegistry mapStateRegistry;
        private HubStructureRegistry hubStructureRegistry;
        private SaveService saveService;
        private Scene currentScene;
        private MapRoot currentRoot;
        private bool travelInProgress;

        private MapDefinition startOverrideMap;
        private Vector2? startOverridePosition;

        public MapDefinition CurrentMap => currentRoot != null ? currentRoot.Definition : null;

        /// <summary>맵 전환(페이드·씬 로드) 도중인지. 이 동안은 아이템 사용 등 일부 입력을 건너뛴다.</summary>
        public bool IsTraveling => travelInProgress;

        /// <summary>맵 이동이 끝날 때마다 (현재 맵 정의와 함께) 발행한다. DebugHud 등이 구독한다.</summary>
        public event Action<MapDefinition> MapChanged;

        private void Awake()
        {
            if (screenFader == null) Debug.LogError("[MapLoader] screenFader 참조가 비어 있습니다.", this);
            if (cameraConfiner == null) Debug.LogError("[MapLoader] cameraConfiner 참조가 비어 있습니다.", this);
            if (playerBody == null) Debug.LogError("[MapLoader] playerBody 참조가 비어 있습니다.", this);
            if (playerController == null) Debug.LogError("[MapLoader] playerController 참조가 비어 있습니다.", this);
            if (playerInput == null) Debug.LogError("[MapLoader] playerInput 참조가 비어 있습니다.", this);
            if (playerInventory == null) Debug.LogError("[MapLoader] playerInventory 참조가 비어 있습니다.", this);
            if (playerSurvival == null) Debug.LogError("[MapLoader] playerSurvival 참조가 비어 있습니다.", this);
            if (playerStatusEffects == null) Debug.LogError("[MapLoader] playerStatusEffects 참조가 비어 있습니다.", this);
            if (startMap == null) Debug.LogError("[MapLoader] startMap 참조가 비어 있습니다.", this);
        }

        private void Start()
        {
            worldClock = GameServices.WorldClock;
            mapStateRegistry = GameServices.MapStateRegistry;
            hubStructureRegistry = GameServices.HubStructureRegistry;
            saveService = GameServices.SaveService;
            Application.quitting += HandleApplicationQuitting;

            MapDefinition target = startOverrideMap != null ? startOverrideMap : startMap;
            string spawnId = startOverrideMap != null ? "" : startSpawnPointId;
            _ = Travel(target, spawnId, 0, startOverridePosition);
        }

        private void OnDestroy() => Application.quitting -= HandleApplicationQuitting;

        /// <summary>창을 닫거나 Play Mode를 멈출 때도 마지막 상태를 저장한다 (AutoSave가 매번 현재 맵을
        /// 강제로 캡처하므로 여기서 더 할 일은 없다).</summary>
        private void HandleApplicationQuitting() => AutoSave();

        /// <summary>저장 파일을 불러왔을 때 GameBootstrap이 시작 맵·위치를 덮어쓴다 (Start보다 먼저 불린다).</summary>
        public void SetStartOverride(MapDefinition map, Vector2 position)
        {
            startOverrideMap = map;
            startOverridePosition = position;
        }

        public async Awaitable Travel(MapDefinition targetMap, string spawnPointId, int travelMinutes) =>
            await Travel(targetMap, spawnPointId, travelMinutes, null);

        private async Awaitable Travel(MapDefinition targetMap, string spawnPointId, int travelMinutes, Vector2? exactPosition)
        {
            if (targetMap == null)
            {
                GameLog.Warning("MapLoader", "targetMap이 비어 있어 이동을 건너뜁니다.", this);
                return;
            }
            if (travelInProgress)
            {
                GameLog.Warning("MapLoader", "이미 맵 이동 중입니다.", this);
                return;
            }
            travelInProgress = true;
            CancellationToken token = destroyCancellationToken;
            bool fadedOut = false;
            try
            {
                playerInput?.SetLocked(this, true);
                if (screenFader != null)
                {
                    await screenFader.FadeOut(token);
                    fadedOut = true;
                }

                if (currentScene.IsValid())
                {
                    CaptureCurrentMapState();
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);
                    if (unload != null) await Awaitable.FromAsyncOperation(unload, token);
                    currentRoot = null;
                }

                if (travelMinutes > 0) worldClock?.Advance(travelMinutes);

                await LoadMapScene(targetMap.SceneName, token);
                currentRoot = FindMapRoot(currentScene);
                if (currentRoot == null)
                {
                    GameLog.Warning("MapLoader", $"'{targetMap.SceneName}' 씬에 MapRoot가 없습니다.", this);
                }
                else
                {
                    RestoreNewMapState();
                    if (exactPosition.HasValue) PlacePlayerAt(exactPosition.Value);
                    else PlacePlayer(spawnPointId);
                    ConfigureCameraBounds();
                }

                // TODO(6단계): 이 맵에 사망 주머니가 있으면 생성 (DeathBagRecord, ARCHITECTURE.md 4.8)
                AutoSave();

                MapChanged?.Invoke(CurrentMap);
            }
            catch (OperationCanceledException)
            {
                // 오브젝트가 파괴되는 중(씬 종료 등)의 정상적인 취소라 따로 알리지 않는다.
            }
            catch (Exception e)
            {
                GameLog.Warning("MapLoader", $"'{targetMap.SceneName}'으로 이동 중 오류가 나 중단합니다: {e}", this);
            }
            finally
            {
                // 화면이 까맣게 꺼진 채로 멈추지 않도록, 실패했어도 반드시 다시 밝힌다. screenFader가
                // 오브젝트 파괴 중이면 취소 예외가 아닌 다른 예외(MissingReferenceException 등)도 날 수
                // 있으므로 널리 잡는다 — 이 finally를 벗어나면 아래 SetLocked(false)/travelInProgress
                // 리셋까지 같이 건너뛰어 버린다.
                if (fadedOut && screenFader != null)
                {
                    try { await screenFader.FadeIn(token); }
                    catch (Exception e)
                    {
                        GameLog.Warning("MapLoader", $"페이드 인 복구 중 오류: {e}", this);
                    }
                }
                playerInput?.SetLocked(this, false);
                travelInProgress = false;
            }
        }

        private void CaptureCurrentMapState()
        {
            if (mapStateRegistry == null || currentRoot == null || currentRoot.Definition == null) return;
            IPersistentObject[] objects = currentRoot.GetComponentsInChildren<IPersistentObject>(true);
            mapStateRegistry.Capture(currentRoot.Definition.Id, objects, worldClock?.TotalMinutes ?? 0);
        }

        private void RestoreNewMapState()
        {
            if (mapStateRegistry == null || currentRoot == null || currentRoot.Definition == null) return;
            IPersistentObject[] objects = currentRoot.GetComponentsInChildren<IPersistentObject>(true);
            mapStateRegistry.Restore(currentRoot.Definition.Id, objects, worldClock?.TotalMinutes ?? 0);
        }

        /// <summary>맵 이동 없이 저장해야 할 때(잠자기 등) 바깥에서 부른다.</summary>
        public void SaveNow() => AutoSave();

        private void AutoSave()
        {
            if (saveService == null || mapStateRegistry == null || CurrentMap == null) return;

            // 지금 맵의 채집물·문·화로·보관함 등 라이브 상태를 레지스트리에 먼저 밀어 넣는다 — 그러지
            // 않으면 CaptureCurrentMapState/BuildGrid.SyncRegistry가 마지막으로 돈 시점(맵 이동) 이후의
            // 변화(잠자기 중 문을 열어 둔 채 저장 등)가 저장 파일에 빠진다.
            CaptureCurrentMapState();
            GameServices.BuildGrid?.SyncNow();

            var data = new WorldSaveData
            {
                settings = worldClock?.Settings,
                clockTotalMinutes = worldClock?.TotalMinutes ?? 0,
                currentMapId = CurrentMap.Id,
                playerX = playerBody != null ? playerBody.position.x : 0f,
                playerY = playerBody != null ? playerBody.position.y : 0f,
                inventorySlots = new List<InventorySlotData>(),
                maps = new List<MapSaveData>()
            };

            if (playerInventory != null)
                foreach ((string itemId, int count, long spoilAtMinute, int durability, int slotIndex) in playerInventory.EnumerateSlots())
                    data.inventorySlots.Add(new InventorySlotData { itemId = itemId, count = count, spoilAtMinute = spoilAtMinute, durability = durability, slotIndex = slotIndex });

            if (hubStructureRegistry != null)
                data.hubStructures = new List<StructureSaveData>(hubStructureRegistry.Current);

            if (playerSurvival != null)
                foreach ((string id, float value) in playerSurvival.EnumerateStats())
                    data.stats.Add(new StatSaveData { id = id, value = value });

            if (playerStatusEffects != null)
                foreach ((string id, long untilMinute) in playerStatusEffects.EnumerateEffects())
                    data.statusEffects.Add(new StatusEffectSaveData { id = id, untilMinute = untilMinute });

            foreach (string mapId in mapStateRegistry.MapIds)
            {
                var mapData = new MapSaveData
                {
                    mapId = mapId,
                    lastVisitedMinute = mapStateRegistry.GetLastVisitedMinute(mapId),
                    objects = new List<ObjectSaveData>()
                };
                foreach (KeyValuePair<string, ObjectStateRecord> pair in mapStateRegistry.GetObjects(mapId))
                    mapData.objects.Add(new ObjectSaveData
                    {
                        persistentId = pair.Key,
                        state = pair.Value.State,
                        untilMinute = pair.Value.UntilMinute,
                        extra = pair.Value.Extra
                    });
                data.maps.Add(mapData);
            }

            saveService.Save(data);
        }

        private async Awaitable LoadMapScene(string sceneName, CancellationToken token)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                GameLog.Warning("MapLoader", $"씬 '{sceneName}'을 로드할 수 없습니다. Build Profiles에 등록되었는지 확인하세요.", this);
                return;
            }
            await Awaitable.FromAsyncOperation(op, token);
            currentScene = SceneManager.GetSceneByName(sceneName);
            if (currentScene.IsValid()) SceneManager.SetActiveScene(currentScene);
        }

        private static MapRoot FindMapRoot(Scene scene)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MapRoot mapRoot = root.GetComponent<MapRoot>();
                if (mapRoot != null) return mapRoot;
            }
            return null;
        }

        private void PlacePlayer(string spawnPointId)
        {
            if (playerBody == null || currentRoot == null) return;
            SpawnPoint spawn = currentRoot.FindSpawnPoint(spawnPointId);
            if (spawn == null)
            {
                GameLog.Warning("MapLoader", $"스폰 지점 '{spawnPointId}'를 찾지 못했습니다.", this);
                return;
            }
            PlacePlayerAt(spawn.transform.position);
        }

        private void PlacePlayerAt(Vector2 position)
        {
            if (playerController != null) playerController.TeleportTo(position);
            else if (playerBody != null) playerBody.position = position;
        }

        private void ConfigureCameraBounds()
        {
            if (cameraConfiner == null || currentRoot == null) return;
            cameraConfiner.BoundingShape2D = currentRoot.CameraBounds;
            cameraConfiner.InvalidateBoundingShapeCache();
        }
    }
}
