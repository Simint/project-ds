using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    /// <summary>고를 수 있는 난이도 목록 (쉬움·보통·어려움 순서로 둔다). 저장된 id로 정의를 찾는다.</summary>
    [CreateAssetMenu(menuName = "Game/World/Difficulty Database")]
    public sealed class DifficultyDatabase : ScriptableObject
    {
        [SerializeField] private List<DifficultyDefinition> difficulties = new List<DifficultyDefinition>();
        [Tooltip("새 게임 화면에서 처음 골라져 있는 난이도, 저장된 id를 못 찾을 때도 쓴다.")]
        [SerializeField] private DifficultyDefinition defaultDifficulty;

        public IReadOnlyList<DifficultyDefinition> Difficulties => difficulties;
        public DifficultyDefinition Default => defaultDifficulty;

        public DifficultyDefinition Find(string id)
        {
            for (int i = 0; i < difficulties.Count; i++)
                if (difficulties[i] != null && difficulties[i].Id == id) return difficulties[i];
            return defaultDifficulty;
        }
    }
}
