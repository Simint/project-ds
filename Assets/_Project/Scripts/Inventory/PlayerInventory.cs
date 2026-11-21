using System.Collections.Generic;
using Game.Core;
using Game.Items;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 플레이어가 들고 있는 InventoryModel과 아이템 정의(ItemDatabase)를 잇는다. 얻은 아이템은 바로
    /// 가방에 들어가고, 못 담으면 그만큼만 Pickup으로 땅에 떨어진다 (docs/design/GAME_DESIGN.md 7.1).
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private int slotCount = 20;
        [SerializeField] private float weightLimit = 50f;

        private InventoryModel model;

        // GameBootstrap이 Awake에서 저장 파일을 곧바로 복원하므로, 다른 컴포넌트의 Awake 순서에
        // 기대지 않도록 처음 접근하는 시점에 만든다 (직렬화 필드는 Awake 전에 이미 채워져 있다).
        public InventoryModel Model => model ??= new InventoryModel(slotCount);
        public float WeightLimit => weightLimit;

        private void Awake()
        {
            if (itemDatabase == null) Debug.LogError("[PlayerInventory] itemDatabase 참조가 비어 있습니다.", this);
        }

        /// <summary>얻은 아이템을 가방에 넣는다. 다 못 담으면 남은 만큼 Pickup을 떨어뜨린다.</summary>
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

            int leftover = Model.TryAdd(itemId, count, def.MaxStackSize, def.Weight, spoilAt);
            if (leftover > 0) DropToGround(itemId, leftover);
        }

        private void DropToGround(string itemId, int count)
        {
            if (pickupPrefab == null)
            {
                GameLog.Warning("PlayerInventory", "pickupPrefab이 비어 있어 바닥에 떨어뜨리지 못했습니다.", this);
                return;
            }
            Pickup pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            pickup.Initialize(itemId, count);
        }

        public IEnumerable<(string itemId, int count, long spoilAtMinute)> EnumerateSlots()
        {
            foreach (InventorySlot slot in Model.Slots)
                if (slot != null && !slot.IsEmpty) yield return (slot.ItemId, slot.Count, slot.SpoilAtMinute);
        }

        /// <summary>저장 파일에서 불러올 때 현재 가방을 비우고 그대로 다시 채운다.</summary>
        public void RestoreSlots(IEnumerable<(string itemId, int count, long spoilAtMinute)> entries)
        {
            Model.Clear();
            if (entries == null) return;
            foreach ((string itemId, int count, long spoilAtMinute) in entries)
            {
                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
                if (def == null) continue;
                Model.TryAdd(itemId, count, def.MaxStackSize, def.Weight, spoilAtMinute);
            }
        }
    }
}
