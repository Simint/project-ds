using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 전투 공용 수치 (docs/design/GAME_DESIGN.md 6.5, MVP 6단계). 무기별 수치는
    /// ItemDefinition.Weapon에, 적별 수치는 CreatureDefinition(AI/)에 둔다. 여기는 짐승 손 발톱
    /// 공격처럼 무기 아이템이 없는 공격과, 체력바 표시 시간처럼 어느 한쪽에도 속하지 않는 수치만
    /// 담는다. 전부 임시값으로 시작해 플레이하며 조절한다 (.claude/rules/csharp.md "수치와 이름").
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Combat/Combat Settings")]
    public sealed class CombatSettings : ScriptableObject
    {
        [Header("짐승 손 발톱 공격 (사람 손은 무기 없이 못 때린다, docs/design 3.1)")]
        [SerializeField] private float beastClawDamage = 0f;
        [SerializeField] private float beastClawCooldownSeconds = 0f;
        [SerializeField] private float beastClawStaminaCost = 0f;
        [SerializeField] private float beastClawRangeTiles = 0f;
        [SerializeField] private float beastClawKnockbackForce = 0f;

        [Header("적 체력바 (맞았을 때만 잠깐)")]
        [SerializeField] private float enemyHealthBarVisibleSeconds = 0f;

        [Header("앉아 걷기 (시야 감지, docs/design 6.5)")]
        [SerializeField] private float crouchSightMultiplier = 1f;

        public float BeastClawDamage => beastClawDamage;
        public float BeastClawCooldownSeconds => beastClawCooldownSeconds;
        public float BeastClawStaminaCost => beastClawStaminaCost;
        public float BeastClawRangeTiles => beastClawRangeTiles;
        public float BeastClawKnockbackForce => beastClawKnockbackForce;

        public float EnemyHealthBarVisibleSeconds => enemyHealthBarVisibleSeconds;

        /// <summary>앉아 있을 때 적 시야 거리에 곱하는 배율 (1 = 영향 없음). AI(AI/)가 CreatureDefinition의
        /// 기본 시야 거리에 곱해 쓴다.</summary>
        public float CrouchSightMultiplier => crouchSightMultiplier;
    }
}
