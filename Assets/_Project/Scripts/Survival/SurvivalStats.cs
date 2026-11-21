using System;
using System.Collections.Generic;

namespace Game.Survival
{
    /// <summary>
    /// 생존 스탯 전체 모델 (ARCHITECTURE.md 4.3). MonoBehaviour 없이 테스트할 수 있는 순수 C# 클래스다.
    /// Tick(gameHours)가 모든 스탯에 (기본 changePerGameHour × 모디파이어)를 한 번에 적용한다 —
    /// WorldClock.MinutePassed(1/60시간)와 Advance(건너뛴 시간 전체)가 같은 메서드를 쓴다
    /// (ARCHITECTURE.md 4.1). HP와 스태미나는 기본값이 0이라 Tick만으로는 바뀌지 않고, PlayerSurvival이
    /// 각자의 규칙으로 직접 Add/SetValue한다.
    /// </summary>
    public sealed class SurvivalStats
    {
        private readonly Dictionary<string, SurvivalStat> statsById = new Dictionary<string, SurvivalStat>();

        // 출처(sourceKey)별로 등록된 모디파이어. statId -> (sourceKey -> modifier).
        private readonly Dictionary<string, Dictionary<string, StatModifier>> modifiers =
            new Dictionary<string, Dictionary<string, StatModifier>>();

        /// <summary>스탯 값이 바뀔 때마다 발행한다. HUD가 구독해서 다시 그린다.</summary>
        public event Action Changed;

        public IReadOnlyCollection<SurvivalStat> All => statsById.Values;

        public SurvivalStats(SurvivalStatDatabase database)
        {
            if (database == null) return;
            foreach (SurvivalStatDefinition def in database.Stats)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                statsById[def.Id] = new SurvivalStat(def, def.StartValue);
            }
        }

        public SurvivalStat Get(string statId) =>
            !string.IsNullOrEmpty(statId) && statsById.TryGetValue(statId, out SurvivalStat stat) ? stat : null;

        public void SetModifier(string sourceKey, string statId, StatModifier modifier)
        {
            if (!modifiers.TryGetValue(statId, out Dictionary<string, StatModifier> bySource))
            {
                bySource = new Dictionary<string, StatModifier>();
                modifiers[statId] = bySource;
            }
            bySource[sourceKey] = modifier;
        }

        public void ClearModifier(string sourceKey, string statId)
        {
            if (modifiers.TryGetValue(statId, out Dictionary<string, StatModifier> bySource))
                bySource.Remove(sourceKey);
        }

        public float EffectiveRatePerHour(string statId)
        {
            SurvivalStat stat = Get(statId);
            float rate = stat != null ? stat.Definition.ChangePerGameHour : 0f;
            if (!modifiers.TryGetValue(statId, out Dictionary<string, StatModifier> bySource)) return rate;

            float multiplier = 1f;
            float additive = 0f;
            foreach (StatModifier modifier in bySource.Values)
            {
                multiplier *= modifier.Multiplier;
                additive += modifier.AdditivePerHour;
            }
            return rate * multiplier + additive;
        }

        /// <summary>모든 스탯에 (시간당 변화율 × gameHours)을 적용한다. 변화가 없는 스탯은 건드리지 않는다.</summary>
        public void Tick(float gameHours)
        {
            if (gameHours <= 0f) return;
            bool changed = false;
            foreach (SurvivalStat stat in statsById.Values)
            {
                float rate = EffectiveRatePerHour(stat.Definition.Id);
                if (rate == 0f) continue;
                stat.Add(rate * gameHours);
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        /// <summary>HP·스태미나처럼 Tick이 아니라 직접 계산하는 스탯이 값을 바꾼 뒤 HUD에 알릴 때 쓴다.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        public void RestoreValues(IEnumerable<(string id, float value)> values)
        {
            if (values == null) return;
            foreach ((string id, float value) in values)
                Get(id)?.SetValue(value);
            Changed?.Invoke();
        }
    }
}
