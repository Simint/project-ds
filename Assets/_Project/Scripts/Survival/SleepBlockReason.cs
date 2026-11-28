namespace Game.Survival
{
    /// <summary>잠을 잘 수 없는 이유 (docs/design/GAME_DESIGN.md 8 침대). None이면 잘 수 있다.</summary>
    public enum SleepBlockReason
    {
        None,
        Starving,
        Bleeding,
        Fracture
    }
}
