using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 스포너 한 곳이 몇 마리를 어느 범위에 내보내는지 (docs/design/GAME_DESIGN.md 6.5). 지역별 적 수는
    /// 밸런스 수치라 공개 씬의 인스펙터가 아니라 비공개 Data/ 에셋에 둔다 (CLAUDE.md "비공개 서브모듈").
    /// 적 하나의 능력치는 CreatureDefinition, 무리의 규모는 이 정의가 맡는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/AI/Spawn Group Definition")]
    public sealed class SpawnGroupDefinition : ScriptableObject
    {
        [SerializeField] [Min(0)] private int maxAliveCount = 0;

        [Tooltip("스포너 위치에서 무작위로 흩어지는 반경 (타일). 0이면 모두 같은 자리.")]
        [SerializeField] [Min(0f)] private float spawnRadiusTiles = 0f;

        public int MaxAliveCount => maxAliveCount;
        public float SpawnRadiusTiles => spawnRadiusTiles;
    }
}
