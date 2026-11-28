using System;
using System.Collections.Generic;
using System.Threading;
using Game.Building;
using Game.Core;
using Game.Inventory;
using Game.Items;
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
    /// Boot 씬에 하나 두고, Awake에서 자기 참조만 캐시한다. 타이틀에서 시작을 고르면 BeginGame()으로 시작 맵에 간다. 맵 이동이
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
        [SerializeField] private DeathBagPickup deathBagPickupPrefab;

        private IWorldClock worldClock;
        private MapStateRegistry mapStateRegistry;
        private HubStructureRegistry hubStructureRegistry;
        private DeathBagRegistry deathBagRegistry;
        private SaveService saveService;
        private Scene currentScene;
        private MapRoot currentRoot;
        private bool travelInProgress;

        private MapDefinition startOverrideMap;
        private Vector2? startOverridePosition;

        // 이동하면서 내린(떠난) 맵과 그 직전 플레이어 위치. 이동 도중 사망(사망 주머니 위치)과
        // 새 맵 로드 실패 시 되돌아가기에 쓴다.
        private MapDefinition departedMap;
        private Vector2 departedPosition;

        public MapDefinition CurrentMap => currentRoot != null ? currentRoot.Definition : null;

        /// <summary>플레이어가 지금 "있는" 맵과 위치. 평소에는 현재 맵과 실제 위치이고, 이동 도중(떠난 맵을
        /// 내린 뒤 새 맵이 준비되기 전)에는 떠난 맵의 마지막 위치다. 둘 다 없으면(게임 시작 전) false.</summary>
        public bool TryGetPlayerLocation(out MapDefinition map, out Vector2 position)
        {
            if (CurrentMap != null)
            {
                map = CurrentMap;
                position = playerBody != null ? playerBody.position : Vector2.zero;
                return true;
            }
            map = departedMap;
            position = departedPosition;
            return map != null;
        }

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
            deathBagRegistry = GameServices.DeathBagRegistry;
            saveService = GameServices.SaveService;
            Application.quitting += HandleApplicationQuitting;
        }

        /// <summary>타이틀에서 새 게임·이어하기를 고른 뒤 GameBootstrap이 부른다. 그전에는 맵을 불러오지 않는다
        /// (불러오기면 SetStartOverride가 먼저 불려 있다).</summary>
        public void BeginGame()
        {
            if (playerBody != null) playerBody.simulated = true;
            MapDefinition target = startOverrideMap != null ? startOverrideMap : startMap;
            string spawnId = startOverrideMap != null ? "" : startSpawnPointId;
            _ = Travel(target, spawnId, 0, startOverridePosition);
        }

        private void OnDestroy() => Application.quitting -= HandleApplicationQuitting;

        /// <summary>창을 닫거나 Play Mode를 멈출 때도 마지막 상태를 저장한다 (AutoSave가 매번 현재 맵을
        /// 강제로 캡처하므로 여기서 더 할 일은 없다).</summary>
        private void HandleApplicationQuitting() => AutoSave();

        /// <summary>저장 파일을 불러왔을 때 GameBootstrap이 시작 맵·위치를 덮어쓴다 (BeginGame보다 먼저 불린다).</summary>
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
            bool arrived = false;
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
                    // 은신처 건설물은 씬을 내리기 전, 모든 구조물이 살아 있을 때 기록한다. 내리는 도중
                    // (BuildGrid.OnDisable)에는 자식 구조물이 먼저 파괴돼 기록에서 빠질 수 있다.
                    GameServices.BuildGrid?.SyncNow();
                    // 제대로 들어가 있던 맵만 기억한다(직전 이동이 실패해 맵이 없는 상태면 그 전 기억을 지킨다).
                    if (CurrentMap != null)
                    {
                        departedMap = CurrentMap;
                        departedPosition = playerBody != null ? playerBody.position : Vector2.zero;
                    }
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);
                    if (unload != null) await Awaitable.FromAsyncOperation(unload, token);
                    currentRoot = null;
                }

                if (travelMinutes > 0) worldClock?.Advance(travelMinutes);

                // 이동이 실패해 되돌아가더라도 이미 흐른 이동 시간(Advance)은 되돌리지 않는다 — 길을 나섰다가
                // 돌아온 것으로 본다.
                currentRoot = await LoadMapWithRoot(targetMap, token);
                if (currentRoot == null && departedMap != null && departedMap != targetMap)
                {
                    // 플레이어가 빈 곳에 남지 않도록 떠난 맵의 마지막 위치로 한 번만 되돌아간다.
                    Debug.LogError($"[MapLoader] '{targetMap.SceneName}'으로 이동하지 못해 '{departedMap.SceneName}'으로 되돌아갑니다.", this);
                    currentRoot = await LoadMapWithRoot(departedMap, token);
                    if (currentRoot != null) exactPosition = departedPosition;
                }

                if (currentRoot != null)
                {
                    RestoreNewMapState();
                    if (exactPosition.HasValue) PlacePlayerAt(exactPosition.Value);
                    else PlacePlayer(spawnPointId);
                    ConfigureCameraBounds();
                    SpawnDeathBagIfPresent();
                    AutoSave();
                    arrived = true;
                }
            }
            catch (OperationCanceledException)
            {
                // 오브젝트가 파괴되는 중(씬 종료 등)의 정상적인 취소라 따로 알리지 않는다.
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapLoader] '{targetMap.SceneName}'으로 이동 중 오류가 나 중단합니다: {e}", this);
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

            // 이동이 완전히 끝난 뒤(travelInProgress가 풀린 뒤)에 알린다. 이동 중에 발행하면 구독자가 그 안에서
            // 다시 Travel을 부를 때(예: 이동 중 사망 → PlayerDeath가 은신처로 보냄) "이미 이동 중"으로 무시된다.
            // 실패·취소된 이동은 도착한 것이 아니므로 알리지 않는다.
            if (arrived) MapChanged?.Invoke(CurrentMap);
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

        /// <summary>이 맵에 사망 주머니가 남아 있으면(죽은 자리가 바로 이 맵이면) 만든다
        /// (ARCHITECTURE.md 4.8). 주머니를 열면 DeathBagPickup이 레지스트리를 비우므로, 다음에
        /// 같은 맵에 들어와도 다시 생기지 않는다.</summary>
        private void SpawnDeathBagIfPresent()
        {
            if (deathBagRegistry == null || !deathBagRegistry.HasBag || CurrentMap == null) return;
            if (deathBagRegistry.Current.mapId != CurrentMap.Id) return;
            if (deathBagPickupPrefab == null)
            {
                GameLog.Warning("MapLoader", "deathBagPickupPrefab이 비어 있어 사망 주머니를 만들지 못했습니다.", this);
                return;
            }

            Vector2 position = new Vector2(deathBagRegistry.Current.x, deathBagRegistry.Current.y);
            Instantiate(deathBagPickupPrefab, position, Quaternion.identity);
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

            if (deathBagRegistry != null) data.deathBag = deathBagRegistry.Current;

            if (GameServices.LoreCollection != null)
                data.loreNoteIds = new List<string>(GameServices.LoreCollection.Ids);

            if (GameServices.Weather != null && GameServices.Weather.Current != null)
            {
                data.weatherId = GameServices.Weather.Current.Id;
                data.weatherEndsAtMinute = GameServices.Weather.EndsAtMinute;
                data.rainMinutesTotal = GameServices.Weather.RainMinutesTotal;
            }

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

        /// <summary>맵 씬을 불러오고 MapRoot를 찾는다. 씬을 못 불러오거나 MapRoot가 없으면 null — MapRoot 없는
        /// 씬은 쓸 수 없으므로 바로 내려서, 되돌아가기로 다른 맵을 불러와도 맵 씬이 둘 겹쳐 뜨지 않게 한다.</summary>
        private async Awaitable<MapRoot> LoadMapWithRoot(MapDefinition map, CancellationToken token)
        {
            if (!await LoadMapScene(map.SceneName, token)) return null;
            MapRoot root = FindMapRoot(currentScene);
            if (root != null) return root;

            Debug.LogError($"[MapLoader] '{map.SceneName}' 씬에 MapRoot가 없어 내립니다.", this);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);
            if (unload != null) await Awaitable.FromAsyncOperation(unload, token);
            currentScene = default;
            return null;
        }

        /// <summary>씬을 덧붙여 불러오고 활성 씬으로 만든다. 불러오지 못하면 false (currentScene은 비운다).</summary>
        private async Awaitable<bool> LoadMapScene(string sceneName, CancellationToken token)
        {
            currentScene = default;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[MapLoader] 씬 '{sceneName}'을 로드할 수 없습니다. Build Profiles에 등록되었는지 확인하세요.", this);
                return false;
            }
            await Awaitable.FromAsyncOperation(op, token);
            currentScene = SceneManager.GetSceneByName(sceneName);
            // SceneName이 경로 형식이라 이름으로 못 찾으면, 방금 덧붙인(목록 맨 끝) 씬을 쓴다 — 그래야 실제로
            // 올라온 씬을 잃어버려 내리지 못하는 일이 없다.
            if (!currentScene.IsValid()) currentScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            if (!currentScene.IsValid()) return false;
            SceneManager.SetActiveScene(currentScene);
            return true;
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
