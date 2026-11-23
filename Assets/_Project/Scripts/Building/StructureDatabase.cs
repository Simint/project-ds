using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>모든 StructureDefinition의 목록. 건설 모드 목록과 저장 불러오기(definitionId → 정의)에 쓴다.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Structure Database")]
    public sealed class StructureDatabase : ScriptableObject
    {
        [SerializeField] private StructureDefinition[] structures = System.Array.Empty<StructureDefinition>();

        private Dictionary<string, StructureDefinition> lookup;

        public IReadOnlyList<StructureDefinition> Structures => structures;

        public StructureDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out StructureDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, StructureDefinition>();
            foreach (StructureDefinition structure in structures)
            {
                if (structure == null || string.IsNullOrEmpty(structure.Id)) continue;
                if (!lookup.TryAdd(structure.Id, structure))
                    Debug.LogError($"[StructureDatabase] 구조물 id '{structure.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
