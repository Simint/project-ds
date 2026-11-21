using System;
using System.Collections.Generic;
using System.Threading;
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
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerSurvival playerSurvival;
        [SerializeField] private PlayerStatusEffects playerStatusEffects;
        [SerializeField] private MapDefinition startMap;
        [SerializeField] private string startSpawnPointId = "";

        private IWorldClock worldClock;
        private MapStateRegistry mapStateRegistry;
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
            saveService = GameServices.SaveService;

            MapDefinition target = startOverrideMap != null ? startOverrideMap : startMap;
            string spawnId = startOverrideMap != null ? "" : startSpawnPointId;
            _ = Travel(target, spawnId, 0, startOverridePosition);
        }

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
            try
            {
                CancellationToken token = destroyCancellationToken;

                playerInput?.SetLocked(true);
                if (screenFader != null) await screenFader.FadeOut(token);

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
                if (screenFader != null) await screenFader.FadeIn(token);
            }
            finally
            {
                playerInput?.SetLocked(false);
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

        private void AutoSave()
        {
            if (saveService == null || mapStateRegistry == null || CurrentMap == null) return;

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
                foreach ((string itemId, int count, long spoilAtMinute) in playerInventory.EnumerateSlots())
                    data.inventorySlots.Add(new InventorySlotData { itemId = itemId, count = count, spoilAtMinute = spoilAtMinute });

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
            if (playerBody == null) return;
            playerBody.position = position;
            playerBody.linearVelocity = Vector2.zero;
        }

        private void ConfigureCameraBounds()
        {
            if (cameraConfiner == null || currentRoot == null) return;
            cameraConfiner.BoundingShape2D = currentRoot.CameraBounds;
            cameraConfiner.InvalidateBoundingShapeCache();
        }
    }
}
