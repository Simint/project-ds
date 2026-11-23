using Game.Player;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// PlayerController.Landed를 구독해 낙하 피해를 주고, 더 높으면 골절을 건다
    /// (docs/design/GAME_DESIGN.md 5.4). 수치는 SurvivalSettings.
    /// </summary>
    public sealed class FallDamage : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private SurvivalSettings settings;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerStatusEffects statusEffects;

        private void Awake()
        {
            if (controller == null) Debug.LogError("[FallDamage] controller 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[FallDamage] settings 참조가 비어 있습니다.", this);
            if (survival == null) Debug.LogError("[FallDamage] survival 참조가 비어 있습니다.", this);
            if (statusEffects == null) Debug.LogError("[FallDamage] statusEffects 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (controller != null) controller.Landed += HandleLanded;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Landed -= HandleLanded;
        }

        private void HandleLanded(float fallDistanceTiles)
        {
            if (settings == null || survival == null) return;
            if (fallDistanceTiles <= settings.FallDamageThresholdTiles) return;

            float excess = fallDistanceTiles - settings.FallDamageThresholdTiles;
            SurvivalStat hp = survival.Stats.Get(StatIds.Hp);
            hp?.Add(-excess * settings.FallDamagePerExcessTile);
            survival.Stats.NotifyChanged();
            // 다음 분이 지날 때까지 기다리지 않고 지금 바로 사망 판정한다 (ApplyHealthRule만 믿으면 늦어진다).
            survival.CheckDeath();

            if (fallDistanceTiles >= settings.FractureFallThresholdTiles)
                statusEffects?.Apply(StatusEffectIds.Fracture);
        }
    }
}
