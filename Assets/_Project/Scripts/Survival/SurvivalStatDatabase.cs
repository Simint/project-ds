using System.Collections.Generic;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 모든 SurvivalStatDefinition의 목록 (ARCHITECTURE.md 4.3). HUD가 이 목록을 읽어 스탯 바를
    /// 자동으로 만든다 (add-survival-stat 스킬 4절) — 스탯이 늘어도 HUD 코드를 고치지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Survival/Survival Stat Database")]
    public sealed class SurvivalStatDatabase : ScriptableObject
    {
        [SerializeField] private SurvivalStatDefinition[] stats = System.Array.Empty<SurvivalStatDefinition>();

        private Dictionary<string, SurvivalStatDefinition> lookup;

        public IReadOnlyList<SurvivalStatDefinition> Stats => stats;

        public SurvivalStatDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out SurvivalStatDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, SurvivalStatDefinition>();
            foreach (SurvivalStatDefinition stat in stats)
            {
                if (stat == null || string.IsNullOrEmpty(stat.Id)) continue;
                if (!lookup.TryAdd(stat.Id, stat))
                    Debug.LogError($"[SurvivalStatDatabase] 스탯 id '{stat.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
