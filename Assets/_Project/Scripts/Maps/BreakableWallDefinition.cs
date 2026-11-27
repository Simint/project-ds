using Game.Core;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 부술 수 있는 벽 한 종류(막힌 통로, 판자 바리케이드 등)의 정의 (docs/design/GAME_DESIGN.md 4.3의
    /// 7단계 범위). 몇 번 쳐야 부서지는지(내구)는 밸런스 수치라 Data/ 에셋에 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Maps/Breakable Wall Definition")]
    public sealed class BreakableWallDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] [Min(0f)] private float maxDurability = 0f;

        public string Id => id;
        public string DisplayName => displayName;
        public float MaxDurability => maxDurability;
    }
}
