namespace Game.Survival
{
    /// <summary>
    /// 잠을 잘 수 있는지 판정하는 순수 규칙 (docs/design/GAME_DESIGN.md 8 침대). 허기·갈증이 0이거나,
    /// 출혈 중이거나, 골절이 치료되지 않았으면(부목·진통제 등으로 골절 상태가 풀리기 전이면) 못 잔다.
    /// 여러 이유가 겹치면 먼저 처리해야 할 것(출혈 → 골절 → 허기·갈증) 하나만 알린다.
    /// </summary>
    public static class SleepRules
    {
        public static SleepBlockReason Evaluate(bool isStarving, bool hasBleeding, bool hasFracture)
        {
            if (hasBleeding) return SleepBlockReason.Bleeding;
            if (hasFracture) return SleepBlockReason.Fracture;
            if (isStarving) return SleepBlockReason.Starving;
            return SleepBlockReason.None;
        }
    }
}
