using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 근접 무기 아이템의 데이터 (ARCHITECTURE.md 4.4, docs/design/GAME_DESIGN.md 6.1). 사람 손일 때만
    /// 쓸 수 있다(짐승 손은 발톱뿐, 3.1). 총기(Firearm)는 이 단계(MVP 6)에서 넣지 않는다.
    /// </summary>
    [System.Serializable]
    public sealed class WeaponData
    {
        [SerializeField] private WeaponKind kind = WeaponKind.Blunt;
        [SerializeField] private float damage = 0f;
        [SerializeField] private float attackCooldownSeconds = 0f;
        [SerializeField] private float staminaCost = 0f;
        [SerializeField] private float rangeTiles = 0f;
        [SerializeField] private float knockbackForce = 0f;

        public WeaponKind Kind => kind;
        public float Damage => damage;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public float StaminaCost => staminaCost;
        public float RangeTiles => rangeTiles;
        public float KnockbackForce => knockbackForce;
    }
}
