namespace Game.Combat
{
    /// <summary>피해를 받을 수 있는 대상 (플레이어, 적) (ARCHITECTURE.md 4.6).</summary>
    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
