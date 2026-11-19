using System;
using System.Threading;
using Game.Core;
using Game.UI;
using Game.World;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Maps
{
    /// <summary>
    /// 맵 씬 전환의 유일한 창구. GameServices에 등록되어 문·맵 선택에서 호출한다 (ARCHITECTURE.md 3.3).
    /// Boot 씬에 하나 두고, Awake에서 자기 참조만 캐시한 뒤 Start에서 시작 맵으로 이동한다.
    /// </summary>
    public sealed class MapLoader : MonoBehaviour
    {
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private CinemachineConfiner2D cameraConfiner;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private MapDefinition startMap;
        [SerializeField] private string startSpawnPointId = "";

        private IWorldClock worldClock;
        private Scene currentScene;
        private MapRoot currentRoot;
        private bool travelInProgress;

        public MapDefinition CurrentMap => currentRoot != null ? currentRoot.Definition : null;

        /// <summary>맵 이동이 끝날 때마다 (현재 맵 정의와 함께) 발행한다. DebugHud 등이 구독한다.</summary>
        public event Action<MapDefinition> MapChanged;

        private void Awake()
        {
            if (screenFader == null) Debug.LogError("[MapLoader] screenFader 참조가 비어 있습니다.", this);
            if (cameraConfiner == null) Debug.LogError("[MapLoader] cameraConfiner 참조가 비어 있습니다.", this);
            if (playerBody == null) Debug.LogError("[MapLoader] playerBody 참조가 비어 있습니다.", this);
            if (startMap == null) Debug.LogError("[MapLoader] startMap 참조가 비어 있습니다.", this);
        }

        private void Start()
        {
            worldClock = GameServices.WorldClock;
            _ = Travel(startMap, startSpawnPointId, 0);
        }

        public async Awaitable Travel(MapDefinition targetMap, string spawnPointId, int travelMinutes)
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

                // TODO(2단계): 페이드 아웃과 함께 플레이어 입력 잠금 (ARCHITECTURE.md 3.3), 끝나면 해제
                if (screenFader != null) await screenFader.FadeOut(token);

                // TODO(3단계): 현재 맵의 IPersistentObject -> MapStateRegistry.Capture (ARCHITECTURE.md 3.4)
                if (currentScene.IsValid())
                {
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
                    // TODO(3단계): MapStateRegistry.Restore + 리스폰 계산 (ARCHITECTURE.md 3.4)
                    PlacePlayer(spawnPointId);
                    ConfigureCameraBounds();
                }

                // TODO(6단계): 이 맵에 사망 주머니가 있으면 생성 (DeathBagRecord, ARCHITECTURE.md 4.8)
                // TODO(3단계): SaveService 자동 저장 (ARCHITECTURE.md 4.10)

                MapChanged?.Invoke(CurrentMap);
                if (screenFader != null) await screenFader.FadeIn(token);
            }
            finally
            {
                travelInProgress = false;
            }
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
            playerBody.position = spawn.transform.position;
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
