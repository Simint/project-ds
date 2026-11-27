using Game.Core;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 적 하나(좀비·동물)의 정의 (ARCHITECTURE.md 4.7, docs/design/GAME_DESIGN.md 6.3, 6.4, 6.5). 밤·계절
    /// 보정은 낮밤·계절 시스템이 생기는 단계(8단계)에서 추가한다 — 지금은 첫 전투 버전 범위만 담는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/AI/Creature Definition")]
    public sealed class CreatureDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";

        [Header("체력·이동")]
        [SerializeField] private float maxHealth = 0f;
        [SerializeField] private float moveSpeedTiles = 0f;

        [Header("감지 (시야+소음, 6.5)")]
        [SerializeField] private float sightRangeTiles = 0f;

        /// <summary>짐승 손일 때 시야 거리에 곱하는 배율 (1 = 영향 없음). 들개처럼 "같은 개과라 덜
        /// 경계하는" 반응이 있는 적만 1보다 작게 둔다(docs/design 3.1).</summary>
        [SerializeField] private float beastHandSightMultiplier = 1f;

        [Header("공격")]
        [SerializeField] private float attackDamage = 0f;
        [SerializeField] private float attackCooldownSeconds = 0f;
        [SerializeField] private float attackRangeTiles = 0f;
        [SerializeField] private float attackKnockbackForce = 0f;
        [SerializeField] [Range(0f, 1f)] private float attackBleedChance = 0f;

        [Header("밤 보정 (5.6 8단계: 일반 좀비만 1보다 크게)")]
        [SerializeField] [Min(0f)] private float nightSpeedMultiplier = 1f;
        [SerializeField] [Min(0f)] private float nightDamageMultiplier = 1f;
        [SerializeField] [Min(0f)] private float nightSightMultiplier = 1f;

        [Header("리스폰 (채집물과 같은 방식, 6.5)")]
        [SerializeField] private int respawnDays = 0;

        public string Id => id;
        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float MoveSpeedTiles => moveSpeedTiles;
        public float SightRangeTiles => sightRangeTiles;
        public float BeastHandSightMultiplier => beastHandSightMultiplier;
        public float AttackDamage => attackDamage;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public float AttackRangeTiles => attackRangeTiles;
        public float AttackKnockbackForce => attackKnockbackForce;
        public float AttackBleedChance => attackBleedChance;
        public float NightSpeedMultiplier => nightSpeedMultiplier;
        public float NightDamageMultiplier => nightDamageMultiplier;
        public float NightSightMultiplier => nightSightMultiplier;

        /// <summary>0이면 리스폰하지 않는다.</summary>
        public int RespawnDays => respawnDays;
    }
}
