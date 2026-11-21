namespace Game.Survival
{
    /// <summary>현재 걸린 상태 이상 하나(저장 대상). untilMinute 0 = 치료할 때까지 지속.</summary>
    public readonly struct ActiveStatusEffect
    {
        public readonly string Id;
        public readonly long UntilMinute;

        public ActiveStatusEffect(string id, long untilMinute)
        {
            Id = id;
            UntilMinute = untilMinute;
        }
    }
}
