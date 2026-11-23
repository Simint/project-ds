using System;
using System.Collections.Generic;

namespace Game.Survival
{
    /// <summary>
    /// 플레이어에게 현재 걸린 상태 이상 목록 (ARCHITECTURE.md 4.3). MonoBehaviour 없이 테스트할 수
    /// 있는 순수 C# 클래스다. EffectApplied/EffectCleared는 PlayerStatusEffects가 구독해서 이동 배율·
    /// 스탯 모디파이어를 등록·해제한다. SetFromSave는 이벤트를 쏘지 않는다 — 불러온 뒤 그 효과를
    /// PlayerStatusEffects.OnEnable이 한 번 훑어 다시 적용한다(Awake 순서에 기대지 않기 위해).
    /// </summary>
    public sealed class StatusEffects
    {
        private readonly Dictionary<string, ActiveStatusEffect> active = new Dictionary<string, ActiveStatusEffect>();

        public IEnumerable<string> ActiveIds => active.Keys;

        public event Action<string> EffectApplied;
        public event Action<string> EffectCleared;

        public bool Has(string id) => !string.IsNullOrEmpty(id) && active.ContainsKey(id);

        /// <summary>이미 걸려 있으면 다시 걸지 않는다(지속 시간을 늘리지 않음).</summary>
        public void Apply(string id, long nowMinute, long durationMinutes)
        {
            if (string.IsNullOrEmpty(id) || active.ContainsKey(id)) return;
            active[id] = new ActiveStatusEffect(id, durationMinutes > 0 ? nowMinute + durationMinutes : 0);
            EffectApplied?.Invoke(id);
        }

        public void Cure(string id)
        {
            if (!string.IsNullOrEmpty(id) && active.Remove(id))
                EffectCleared?.Invoke(id);
        }

        /// <summary>durationMinutes>0으로 걸린 효과 중 기한이 지난 것을 풀어준다 (질병 등).</summary>
        public void ExpireDue(long nowMinute)
        {
            List<string> expired = null;
            foreach (KeyValuePair<string, ActiveStatusEffect> pair in active)
            {
                if (pair.Value.UntilMinute > 0 && nowMinute >= pair.Value.UntilMinute)
                    (expired ??= new List<string>()).Add(pair.Key);
            }
            if (expired == null) return;
            foreach (string id in expired) Cure(id);
        }

        /// <summary>데이터만 조용히 덮어 쓴다(새로 생긴 효과는 이벤트를 쏘지 않는다 — 불러온 뒤
        /// PlayerStatusEffects.OnEnable이 한 번 훑어 다시 적용한다). 다만 이전에 걸려 있다가 이번엔
        /// 빠진 효과는 EffectCleared를 그대로 쏜다 — 안 그러면 그 효과가 걸어 둔 이동 배율·스탯
        /// 모디파이어가 active에서는 지워져도 PlayerController/SurvivalStats에는 남아 있게 된다.</summary>
        public void SetFromSave(IEnumerable<(string id, long untilMinute)> entries)
        {
            var incoming = new Dictionary<string, long>();
            if (entries != null)
                foreach ((string id, long untilMinute) in entries)
                    if (!string.IsNullOrEmpty(id)) incoming[id] = untilMinute;

            List<string> noLongerActive = null;
            foreach (string id in active.Keys)
                if (!incoming.ContainsKey(id)) (noLongerActive ??= new List<string>()).Add(id);
            if (noLongerActive != null)
                foreach (string id in noLongerActive) Cure(id);

            foreach (KeyValuePair<string, long> pair in incoming)
                active[pair.Key] = new ActiveStatusEffect(pair.Key, pair.Value);
        }

        public IEnumerable<(string id, long untilMinute)> EnumerateForSave()
        {
            foreach (ActiveStatusEffect effect in active.Values)
                yield return (effect.Id, effect.UntilMinute);
        }
    }
}
