namespace Game.Inventory
{
    /// <summary>
    /// 슬롯 하나의 상태(저장 대상). 같은 itemId는 항상 같은 maxStackSize·unitWeight를 쓰므로
    /// 여기 같이 들고 있으면 InventoryModel이 ItemDefinition을 몰라도 무게·한도 계산을 할 수 있다.
    /// </summary>
    public sealed class InventorySlot
    {
        public string ItemId { get; private set; }
        public int Count { get; private set; }
        public int MaxStackSize { get; }
        public float UnitWeight { get; }

        public bool IsEmpty => Count <= 0;

        public InventorySlot(string itemId, int count, int maxStackSize, float unitWeight)
        {
            ItemId = itemId;
            Count = count;
            MaxStackSize = maxStackSize;
            UnitWeight = unitWeight;
        }

        public int RemainingCapacity => MaxStackSize - Count;

        public void Add(int amount) => Count += amount;

        public void Remove(int amount) => Count = System.Math.Max(0, Count - amount);

        public void Clear()
        {
            ItemId = null;
            Count = 0;
        }
    }
}
