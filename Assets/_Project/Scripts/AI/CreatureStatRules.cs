using Game.World;

namespace Game.AI
{
    /// <summary>
    /// 적 능력치에 밤·날씨·난이도 보정을 곱한다 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위 "밤 좀비", 안개, 난이도).
    /// 밤 배율은 정의에 1보다 큰 값이 있는 적(일반 좀비)만 오른다. 순수 함수라 EditMode 테스트로 검증한다.
    /// </summary>
    public static class CreatureStatRules
    {
        public static float MoveSpeed(CreatureDefinition definition, bool isNight) =>
            definition == null ? 0f : definition.MoveSpeedTiles * (isNight ? definition.NightSpeedMultiplier : 1f);

        public static float AttackDamage(CreatureDefinition definition, bool isNight, DifficultyDefinition difficulty) =>
            definition == null ? 0f : DifficultyRules.EnemyDamage(definition.AttackDamage * (isNight ? definition.NightDamageMultiplier : 1f), difficulty);

        public static float SightRange(CreatureDefinition definition, bool isNight, float weatherMultiplier) =>
            definition == null ? 0f : definition.SightRangeTiles * (isNight ? definition.NightSightMultiplier : 1f) * weatherMultiplier;

        public static float MaxHealth(CreatureDefinition definition, DifficultyDefinition difficulty) =>
            definition == null ? 0f : DifficultyRules.EnemyHealth(definition.MaxHealth, difficulty);
    }
}
