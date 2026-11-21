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
        }

        private void HandleDied()
        {
            survival.Revive();
            if (GameServices.MapLoader != null && hubMap != null)
                _ = GameServices.MapLoader.Travel(hubMap, hubSpawnPointId, 0);
        }
    }
}
