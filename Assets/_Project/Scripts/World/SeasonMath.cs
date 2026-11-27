namespace Game.World
{
    /// <summary>계절 순환 계산 (docs/design/GAME_DESIGN.md 5.1, 5.6). 계절 하나의 길이(일)는 월드 설정. 순수 함수.</summary>
    public static class SeasonMath
    {
        public const int SeasonCount = 4;

        public static Season SeasonAt(long totalMinutes, int seasonLengthDays)
        {
            if (seasonLengthDays <= 0 || totalMinutes < 0) return Season.Spring;
            long day = totalMinutes / WorldClock.MinutesPerDay;
            return (Season)(int)(day / seasonLengthDays % SeasonCount);
        }

        /// <summary>지금 계절의 몇째 날인지 (0부터).</summary>
        public static int DayOfSeason(long totalMinutes, int seasonLengthDays)
        {
            if (seasonLengthDays <= 0 || totalMinutes < 0) return 0;
            return (int)(totalMinutes / WorldClock.MinutesPerDay % seasonLengthDays);
        }
    }
}
