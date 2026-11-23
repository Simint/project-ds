using Game.Core;
using Game.Maps;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// PlayerSurvival.Died를 구독해 은신처에서 부활시킨다 (docs/design/GAME_DESIGN.md 5.8, MVP 4단계
    /// 범위: 소지품은 그대로 두고 HP만 되돌린다. 사망 주머니는 6단계).
    /// </summary>
    public sealed class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private MapDefinition hubMap;
        [SerializeField] private string hubSpawnPointId = "";

        private void Awake()
        {
            if (survival == null) Debug.LogError("[PlayerDeath] survival 참조가 비어 있습니다.", this);
            if (hubMap == null) Debug.LogError("[PlayerDeath] hubMap 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (survival != null) survival.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (survival != null) survival.Died -= HandleDied;
            if (GameServices.MapLoader != null) GameServices.MapLoader.MapChanged -= HandleMapChangedAfterDeath;
        }

        private void HandleDied()
        {
            survival.Revive();
            MapLoader loader = GameServices.MapLoader;
            if (loader == null || hubMap == null) return;

            // 맵 이동 도중(페이드·씬 로드 중)에 죽으면 Travel이 "이미 이동 중"이라며 조용히 무시한다 —
            // 지금 진행 중인 이동이 끝날 때까지 기다렸다가 은신처로 되돌린다.
            if (loader.IsTraveling)
                loader.MapChanged += HandleMapChangedAfterDeath;
            else
                _ = loader.Travel(hubMap, hubSpawnPointId, 0);
        }

        private void HandleMapChangedAfterDeath(MapDefinition arrivedAt)
        {
            MapLoader loader = GameServices.MapLoader;
            if (loader == null) return;
            loader.MapChanged -= HandleMapChangedAfterDeath;
            if (hubMap != null) _ = loader.Travel(hubMap, hubSpawnPointId, 0);
        }
    }
}
