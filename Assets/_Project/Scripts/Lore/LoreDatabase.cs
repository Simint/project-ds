using System.Collections.Generic;
using UnityEngine;

namespace Game.Lore
{
    /// <summary>모든 쪽지 정의 목록. 저장된 쪽지 id로 정의를 다시 찾을 때 쓴다 (ARCHITECTURE.md 2장 원칙 2).</summary>
    [CreateAssetMenu(menuName = "Game/Lore/Lore Database")]
    public sealed class LoreDatabase : ScriptableObject
    {
        [SerializeField] private List<LoreNoteDefinition> notes = new List<LoreNoteDefinition>();

        public IReadOnlyList<LoreNoteDefinition> Notes => notes;

        public LoreNoteDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < notes.Count; i++)
                if (notes[i] != null && notes[i].Id == id) return notes[i];
            return null;
        }
    }
}
