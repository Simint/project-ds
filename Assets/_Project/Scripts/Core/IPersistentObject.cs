namespace Game.Core
{
    /// <summary>
    /// 맵에 손으로 놓은, 상태를 저장해야 하는 오브젝트 (ARCHITECTURE.md 3.4). PersistentId와 함께 쓴다.
    /// Restore는 Capture가 돌려준 상태를 그대로 되돌려야 한다(리스코프 치환, csharp.md).
    /// </summary>
    public interface IPersistentObject
    {
        string Id { get; }

        ObjectStateRecord Capture();

        void Restore(ObjectStateRecord record, long nowMinute);
    }
}
