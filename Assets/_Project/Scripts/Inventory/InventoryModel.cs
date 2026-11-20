using System;
using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// 고정 슬롯 + 무게 모델 (ARCHITECTURE.md 4.4, docs/design/GAME_DESIGN.md 7). MonoBehaviour 없이
    /// 테스트할 수 있는 순수 C# 클래스다. 이동 감속은 넣지 않는다(무게는 숫자로만 보여준다, 7장).
    /// 장비 슬롯(머리·몸·손·가방·광원)은 그 장비가 실제로 생기는 단계에서 추가한다.
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly InventorySlot[] slots;

        public int SlotCount => slots.Length;
        public IReadOnlyList<InventorySlot> Slots => slots;

        /// <summary>슬롯이 바뀔 때마다(추가·제거) 발행한다. UI가 구독해서 다시 그린다.</summary>
        public event Action Changed;

        public InventoryModel(int slotCount)
        {
            slots = new InventorySlot[slotCount];
        }

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                foreach (InventorySlot slot in slots)
                    if (slot != null && !slot.IsEmpty) total += slot.Count * slot.UnitWeight;
                return total;
            }
        }

        /// <summary>
        /// 같은 itemId 스택부터 채우고, 남으면 빈 슬롯에 새로 쌓는다. 다 못 담으면 남은 개수를 돌려준다
        /// (호출한 쪽이 Pickup으로 땅에 떨어뜨린다, 7.1).
        /// </summary>
        public int TryAdd(string itemId, int count, int maxStackSize, float unitWeight)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return count;
            int remaining = count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty || slot.ItemId != itemId) continue;
                int add = Math.Min(remaining, slot.RemainingCapacity);
                if (add <= 0) continue;
                slot.Add(add);
                remaining -= add;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty) continue;
                int add = Math.Min(remaining, maxStackSize);
                slots[i] = new InventorySlot(itemId, add, maxStackSize, unitWeight);
                remaining -= add;
            }

            if (remaining != count) Changed?.Invoke();
            return remaining;
        }

        /// <summary>앞 슬롯부터 지정한 개수만큼 치운다. 실제로 치운 개수를 돌려준다(모자라면 있는 만큼만).</summary>
        public int Remove(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return 0;
            int remaining = count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty || slot.ItemId != itemId) continue;
                int take = Math.Min(remaining, slot.Count);
                slot.Remove(take);
                remaining -= take;
                if (slot.IsEmpty) slots[i] = null;
            }

            int removed = count - remaining;
            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        public int CountOf(string itemId)
        {
            int total = 0;
            foreach (InventorySlot slot in slots)
                if (slot != null && !slot.IsEmpty && slot.ItemId == itemId) total += slot.Count;
            return total;
        }

        public void Clear()
        {
            Array.Clear(slots, 0, slots.Length);
            Changed?.Invoke();
        }
    }
}
