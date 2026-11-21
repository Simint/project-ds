namespace Game.Survival
{
    /// <summary>
    /// 스탯의 시간당 변화율에 적용되는 배율·가산값 (ARCHITECTURE.md 4.3). 출처(손 모드, 날씨, 상태
    /// 이상 등)별로 SurvivalStats.SetModifier/ClearModifier로 등록·해제한다. 저장하지 않는다 — 불러온
    /// 뒤 그 출처(손 모드 전환, 환경 갱신, 상태 이상 재적용)가 다시 등록한다.
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly float Multiplier;
        public readonly float AdditivePerHour;

        public StatModifier(float multiplier, float additivePerHour)
        {
            Multiplier = multiplier;
            AdditivePerHour = additivePerHour;
        }

        public static StatModifier Multiply(float multiplier) => new StatModifier(multiplier, 0f);
        public static StatModifier Additive(float additivePerHour) => new StatModifier(1f, additivePerHour);
    }
}
