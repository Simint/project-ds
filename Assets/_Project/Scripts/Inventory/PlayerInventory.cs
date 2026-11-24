using System.Collections.Generic;
using Game.Core;
using Game.Crafting;
using Game.Items;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 플레이어가 들고 있는 InventoryModel과 아이템 정의(ItemDatabase)를 잇는다. 얻은 아이템은 바로
    /// 가방에 들어가고, 못 담으면 그만큼만 Pickup으로 땅에 떨어진다 (docs/design/GAME_DESIGN.md 7.1).
    /// IMaterialSource를 구현해 제작·건설·수리가 가방을 보관함과 같은 방식으로 쓸 수 있게 한다.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour, IMaterialSource
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private PlayerInventorySettings settings;

        private InventoryModel model;

        // GameBootstrap이 Awake에서 저장 파일을 곧바로 복원하므로, 다른 컴포넌트의 Awake 순서에
        // 기대지 않도록 처음 접근하는 시점에 만든다 (직렬화 필드는 Awake 전에 이미 채워져 있다).
        public InventoryModel Model => model ??= new InventoryModel(settings != null ? settings.SlotCount : 0);
        public float WeightLimit => settings != null ? settings.WeightLimit : 0f;

        private void Awake()
        {
            if (itemDatabase == null) Debug.LogError("[PlayerInventory] itemDatabase 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerInventory] settings 참조가 비어 있습니다.", this);
        }

        /// <summary>갓 얻은 아이템을 가방에 넣는다(부패 시각·내구도를 새로 계산한다). 다 못 담으면 남은
        /// 만큼 Pickup을 떨어뜨린다.</summary>
        public void Collect(string itemId, int count)
        {
            if (count <= 0) return;
            ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
            if (def == null)
            {
                GameLog.Warning("PlayerInventory", $"아이템 id '{itemId}'를 찾지 못했습니다.", this);
                return;
            }

            long spoilAt = 0;
            if (def.IsConsumable && def.Consumable.SpoilMinutes > 0)
            {
                long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
                spoilAt = now + def.Consumable.SpoilMinutes;
            }
            int durability = def.IsTool ? def.Tool.MaxDurability : -1;

            CollectWithState(itemId, count, spoilAt, durability);
        }

        /// <summary>이미 부패 시각·내구도가 정해진 스택을 그대로 받는다(보관함 꺼내기, 바닥의 Pickup
        /// 흡수처럼 "이미 있던" 아이템을 옮길 때 쓴다 — 넣을 때와 대칭으로 상태를 잃지 않는다).</summary>
        public void CollectWithState(string itemId, int count, long spoilAtMinute, int durability)
        {
            if (count <= 0) return;
            ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
            if (def == null)
            {
                GameLog.Warning("PlayerInventory", $"아이템 id '{itemId}'를 찾지 못했습니다.", this);
                return;
            }

            int leftover = Model.TryAdd(itemId, count, def.EffectiveMaxStackSize, def.Weight, spoilAtMinute, durability);
            if (leftover > 0) DropToGround(itemId, leftover, spoilAtMinute, durability);
        }

        private void DropToGround(string itemId, int count, long spoilAtMinute, int durability)
        {
            if (pickupPrefab == null)
            {
                GameLog.Warning("PlayerInventory", "pickupPrefab이 비어 있어 바닥에 떨어뜨리지 못했습니다.", this);
                return;
            }
            Pickup pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            pickup.Initialize(itemId, count, spoilAtMinute, durability);
        }

        public int CountOf(string itemId) => Model.CountOf(itemId);

        public int Remove(string itemId, int count) => Model.Remove(itemId, count);

        public IEnumerable<(string itemId, int count, long spoilAtMinute, int durability, int slotIndex)> EnumerateSlots()
        {
            IReadOnlyList<InventorySlot> slots = Model.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && !slot.IsEmpty) yield return (slot.ItemId, slot.Count, slot.SpoilAtMinute, slot.Durability, i);
            }
        }

        /// <summary>저장 파일에서 불러올 때 현재 가방을 비우고 그대로 다시 채운다. slotIndex가 있으면
        /// (0 이상) 그 칸에 정확히 되돌린다 — 옛 버전 저장 파일처럼 slotIndex가 없으면(-1) 예전처럼 빈
        /// 칸부터 채운다.</summary>
        public void RestoreSlots(IEnumerable<(string itemId, int count, long spoilAtMinute, int durability, int slotIndex)> entries)
        {
            Model.Clear();
            if (entries == null) return;
            foreach ((string itemId, int count, long spoilAtMinute, int durability, int slotIndex) in entries)
            {
                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
                if (def == null) continue;
                if (slotIndex >= 0 && slotIndex < Model.SlotCount)
                    Model.SetSlotExact(slotIndex, itemId, count, def.EffectiveMaxStackSize, def.Weight, spoilAtMinute, durability);
                else
                    Model.TryAdd(itemId, count, def.EffectiveMaxStackSize, def.Weight, spoilAtMinute, durability);
            }
        }
    }
}
