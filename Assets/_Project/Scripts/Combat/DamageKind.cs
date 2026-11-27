namespace Game.Combat
{
    /// <summary>피해 종류 (ARCHITECTURE.md 4.6, docs/design/GAME_DESIGN.md 6.1).</summary>
    public enum DamageKind
    {
        Blunt,
        Blade,
        Claw,
        Firearm,
        Radiation,
        /// <summary>도끼·곡괭이·쇠지렛대처럼 도구를 무기로 쓴 타격 (docs/design/GAME_DESIGN.md 6.1 "도구 겸용").</summary>
        Tool
    }
}
