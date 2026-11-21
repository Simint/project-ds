namespace Game.Survival
{
    /// <summary>상태 이상 id 상수 (docs/design/GAME_DESIGN.md 5.4). StatusEffectDefinition.id와 맞춘다.</summary>
    public static class StatusEffectIds
    {
        /// <summary>구조만 둔다. 원인(날붙이·동물 공격)은 6단계에서 붙인다.</summary>
        public const string Bleeding = "bleeding";
        public const string Fracture = "fracture";
        public const string Sickness = "sickness";
    }
}
