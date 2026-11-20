namespace Game.Core
{
    /// <summary>
    /// 손으로 놓은 오브젝트 하나의 저장 상태 (ARCHITECTURE.md 3.4). 상태 번호의 뜻은 컴포넌트마다
    /// 다르다(Harvestable: 0=그대로/1=채집됨, Container: 0=안 털림/1=털림 식).
    /// </summary>
    public readonly struct ObjectStateRecord
    {
        public readonly int State;
        public readonly long UntilMinute;
        public readonly string Extra;

        public ObjectStateRecord(int state, long untilMinute, string extra)
        {
            State = state;
            UntilMinute = untilMinute;
            Extra = extra;
        }
    }
}
