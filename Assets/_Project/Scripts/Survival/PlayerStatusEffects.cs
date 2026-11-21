using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 플레이어의 StatusEffects를 소유하고, 걸리거나 풀릴 때마다 PlayerController의 이동·점프 배율과
    /// SurvivalStats 모디파이어를 등록·해제한다 (ARCHITECTURE.md 4.3). 저장에서 불러온 효과는 데이터만
    /// 조용히 채워지므로(StatusEffects.SetFromSave), OnEnable에서 현재 걸린 효과를 한 번 훑어 다시
    /// 적용한다 — GameBootstrap.Awake가 다른 컴포넌트의 Awake보다 먼저/나중에 돌아도 안전하다.
    /// </summary>
    public sealed class PlayerStatusEffects : MonoBehaviour
    {
        [SerializeField] private StatusEffectDatabase database;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerSurvival survival;

        private StatusEffects effects;

        public StatusEffects Effects => effects ??= new StatusEffects();

        private void Awake()
        {
            if (database == null) Debug.LogError("[PlayerStatusEffects] database 참조가 비어 있습니다.", this);
            if (controller == null) Debug.LogError("[PlayerStatusEffects] controller 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            Effects.EffectApplied += HandleApplied;
            Effects.EffectCleared += HandleCleared;
            foreach (string id in Effects.ActiveIds) HandleApplied(id);

            GameServices.Ready += HandleServicesReady;
            if (GameServices.IsReady) HandleServicesReady();
        }

        private void OnDisable()
        {
            Effects.EffectApplied -= HandleApplied;
            Effects.EffectCleared -= HandleCleared;
            GameServices.Ready -= HandleServicesReady;
            if (GameServices.WorldClock != null) GameServices.WorldClock.MinutePassed -= HandleMinutePassed;
        }

        public void Apply(string id)
        {
            StatusEffectDefinition def = database != null ? database.Find(id) : null;
            if (def == null) return;
            long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            Effects.Apply(id, now, def.DurationMinutes);
        }

        public void Cure(string id) => Effects.Cure(id);

        public void RestoreEffects(System.Collections.Generic.IEnumerable<(string id, long untilMinute)> entries) =>
            Effects.SetFromSave(entries);

        public System.Collections.Generic.IEnumerable<(string id, long untilMinute)> EnumerateEffects() =>
            Effects.EnumerateForSave();

        private void HandleServicesReady()
        {
            if (GameServices.WorldClock != null) GameServices.WorldClock.MinutePassed += HandleMinutePassed;
        }

        private void HandleMinutePassed() =>
            Effects.ExpireDue(GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0);

        private void HandleApplied(string id)
        {
            StatusEffectDefinition def = database != null ? database.Find(id) : null;
            if (def == null) return;
            string source = ModifierSource(id);

            if (controller != null)
            {
                controller.SetSpeedMultiplier(source, def.MoveSpeedMultiplier);
                controller.SetJumpMultiplier(source, def.JumpMultiplier);
            }
            if (survival != null)
                foreach (StatRateModifierEntry entry in def.StatRateModifiers)
                    survival.Stats.SetModifier(source, entry.StatId, new StatModifier(entry.Multiplier, entry.AdditivePerHour));
        }

        private void HandleCleared(string id)
        {
            StatusEffectDefinition def = database != null ? database.Find(id) : null;
            string source = ModifierSource(id);

            if (controller != null)
            {
                controller.ClearSpeedMultiplier(source);
                controller.ClearJumpMultiplier(source);
            }
            if (survival != null && def != null)
                foreach (StatRateModifierEntry entry in def.StatRateModifiers)
                    survival.Stats.ClearModifier(source, entry.StatId);
        }

        private static string ModifierSource(string effectId) => "status:" + effectId;
    }
}
