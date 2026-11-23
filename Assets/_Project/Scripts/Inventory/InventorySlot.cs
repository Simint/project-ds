namespace Game.Inventory
{
    /// <summary>
    /// 슬롯 하나의 상태(저장 대상). 같은 itemId는 항상 같은 maxStackSize·unitWeight를 쓰므로
    /// 여기 같이 들고 있으면 InventoryModel이 ItemDefinition을 몰라도 무게·한도 계산을 할 수 있다.
    /// SpoilAtMinute은 부패하는 아이템(ItemDefinition.Consumable.SpoilMinutes>0)에만 쓰고, 합쳐질 때는
    /// 더 이른 시각을 따른다 (ARCHITECTURE.md 4.4, lazy — 실제 '상함' 판정은 보여주거나 쓸 때 한다).
    /// </summary>
    public sealed class InventorySlot
    {
        public string ItemId { get; private set; }
        public int Count { get; private set; }
        public int MaxStackSize { get; }
        public float UnitWeight { get; }
        public long SpoilAtMinute { get; private set; }

        /// <summary>내구도가 있는 도구만 쓴다. -1 = 내구도 없음(일반 재료·소비 아이템).</summary>
        public int Durability { get; private set; }

        public bool IsEmpty => Count <= 0;

        public InventorySlot(string itemId, int count, int maxStackSize, float unitWeight, long spoilAtMinute = 0, int durability = -1)
        {
            ItemId = itemId;
            Count = count;
            MaxStackSize = maxStackSize;
            UnitWeight = unitWeight;
            SpoilAtMinute = spoilAtMinute;
            Durability = durability;
        }

        public void SetDurability(int value) => Durability = value;

        public int RemainingCapacity => MaxStackSize - Count;

        public void Add(int amount) => Count += amount;

        public void Remove(int amount) => Count = System.Math.Max(0, Count - amount);

        /// <summary>0보다 큰 값끼리는 더 이른(작은) 시각이 이긴다. 기존 값이 0(안 상함)이면 새 값을 그대로 쓴다.</summary>
        public void MergeSpoilAtMinute(long otherSpoilAtMinute)
        {
            if (otherSpoilAtMinute <= 0) return;
            if (SpoilAtMinute <= 0 || otherSpoilAtMinute < SpoilAtMinute) SpoilAtMinute = otherSpoilAtMinute;
        }

        public bool IsSpoiled(long nowMinute) => SpoilAtMinute > 0 && nowMinute >= SpoilAtMinute;

        public void Clear()
        {
            ItemId = null;
            Count = 0;
            SpoilAtMinute = 0;
            Durability = -1;
        }
    }
}
