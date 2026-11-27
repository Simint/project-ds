namespace Game.Save
{
    /// <summary>
    /// 지금 남아 있는 사망 주머니 하나 (ARCHITECTURE.md 4.8, docs/design/GAME_DESIGN.md 5.8). MonoBehaviour
    /// 없이 테스트할 수 있는 순수 C# 클래스다. 마지막 하나만 남으므로 Set은 이전 기록을 그대로 덮어쓴다.
    /// </summary>
    public sealed class DeathBagRegistry
    {
        public DeathBagRecord Current { get; private set; } = new DeathBagRecord();

        public bool HasBag => !string.IsNullOrEmpty(Current.mapId);

        public void Set(DeathBagRecord record) => Current = record ?? new DeathBagRecord();

        /// <summary>회수하면(주머니를 열면) 기록이 지워진다.</summary>
        public void Clear() => Current = new DeathBagRecord();

        public void SetFromSave(DeathBagRecord record) => Current = record ?? new DeathBagRecord();
    }
}
