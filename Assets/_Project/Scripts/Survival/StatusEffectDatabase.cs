using System.Collections.Generic;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>모든 StatusEffectDefinition의 목록. HUD가 읽어 상태 이상 아이콘을 자동으로 만든다.</summary>
    [CreateAssetMenu(menuName = "Game/Survival/Status Effect Database")]
    public sealed class StatusEffectDatabase : ScriptableObject
    {
        [SerializeField] private StatusEffectDefinition[] effects = System.Array.Empty<StatusEffectDefinition>();

        private Dictionary<string, StatusEffectDefinition> lookup;

        public IReadOnlyList<StatusEffectDefinition> Effects => effects;

        public StatusEffectDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out StatusEffectDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, StatusEffectDefinition>();
            foreach (StatusEffectDefinition effect in effects)
            {
                if (effect == null || string.IsNullOrEmpty(effect.Id)) continue;
                if (!lookup.TryAdd(effect.Id, effect))
                    Debug.LogError($"[StatusEffectDatabase] 상태 이상 id '{effect.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
