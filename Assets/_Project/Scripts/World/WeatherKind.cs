namespace Game.World
{
    /// <summary>날씨 종류 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 낙진·산성비는 방사능 지역과 함께 나중에 넣는다.</summary>
    public enum WeatherKind
    {
        Clear,
        Rain,
        Snow,
        Fog,
        Storm
    }
}
