using System.Collections.Generic;
using Game.Core;
using Game.Crafting;
using Game.Inventory;
using Game.Items;
using Game.Player;
using Game.Save;
using Game.UI;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 은신처 보관함 (docs/design/GAME_DESIGN.md 8, MVP 5단계). InventoryModel을 그대로 재사용해 슬롯
    /// 여러 개를 가지며, IMaterialSource로 제작·건설·수리에 쓰인다. 내용물은 StructureInstance.Extra에
    /// JSON으로 바로바로 되돌려 써서(Flush), 씬이 언로드돼도 HubStructureRegistry를 통해 남는다.
    /// </summary>
    public sealed class StorageContainer : MonoBehaviour, IMaterialSource, IInteractable
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private string interactPrompt = "보관함 열기";

        private StructureInstance structureInstance;
        private InventoryModel model;

        public InventoryModel Model => model ??= BuildFromExtra();

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        private void Awake()
        {
            structureInstance = GetComponent<StructureInstance>();
            if (structureInstance == null) Debug.LogError("[StorageContainer] StructureInstance가 없습니다.", this);
            if (itemDatabase == null) Debug.LogError("[StorageContainer] itemDatabase 참조가 비어 있습니다.", this);
        }

        private void OnEnable() => GameServices.StorageRegistry?.Register(this);

        private void OnDisable()
        {
            Flush();
            GameServices.StorageRegistry?.Unregister(this);
        }

        public void Interact(GameObject interactor)
        {
            StorageUI ui = GameServices.StorageUI;
            if (ui == null)
            {
                GameLog.Warning("StorageContainer", "GameServices.StorageUI가 등록되지 않았습니다.", this);
                return;
            }
            ui.Show(this);
        }

        public int CountOf(string itemId) => Model.CountOf(itemId);

        public int Remove(string itemId, int count)
        {
            int removed = Model.Remove(itemId, count);
            if (removed > 0) Flush();
            return removed;
        }

        public IEnumerable<(string itemId, int count, long spoilAtMinute, int durability)> EnumerateSlots()
        {
            foreach (InventorySlot slot in Model.Slots)
                if (slot != null && !slot.IsEmpty) yield return (slot.ItemId, slot.Count, slot.SpoilAtMinute, slot.Durability);
        }

        /// <summary>업그레이드로 칸을 늘릴 자리 — 지금은 아무도 부르지 않는다 (MVP 5단계 범위 밖).</summary>
        public void ExpandSlots(int additionalSlots)
        {
            // TODO(업그레이드 레시피가 생기면): 더 큰 InventoryModel을 만들어 기존 슬롯을 옮겨 담는다.
        }

        private InventoryModel BuildFromExtra()
        {
            int slotCount = structureInstance != null && structureInstance.Definition != null
                ? Mathf.Max(1, structureInstance.Definition.StorageSlotCount)
                : 1;
            var built = new InventoryModel(slotCount);

            string json = structureInstance != null ? structureInstance.Extra : null;
            if (!string.IsNullOrEmpty(json))
            {
                StorageContentsData data = JsonUtility.FromJson<StorageContentsData>(json);
                if (data?.slots != null)
                {
                    foreach (InventorySlotData slot in data.slots)
                    {
                        ItemDefinition def = itemDatabase != null ? itemDatabase.Find(slot.itemId) : null;
                        if (def == null) continue;
                        if (slot.slotIndex >= 0 && slot.slotIndex < built.SlotCount)
                            built.SetSlotExact(slot.slotIndex, slot.itemId, slot.count, def.EffectiveMaxStackSize, def.Weight, slot.spoilAtMinute, slot.durability);
                        else
                            built.TryAdd(slot.itemId, slot.count, def.EffectiveMaxStackSize, def.Weight, slot.spoilAtMinute, slot.durability);
                    }
                }
            }

            built.Changed += FlushModel;
            model = built;
            return built;
        }

        private void FlushModel() => Flush();

        private void Flush()
        {
            if (structureInstance == null || model == null) return;
            var data = new StorageContentsData();
            IReadOnlyList<InventorySlot> slots = model.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty) continue;
                data.slots.Add(new InventorySlotData
                {
                    itemId = slot.ItemId,
                    count = slot.Count,
                    spoilAtMinute = slot.SpoilAtMinute,
                    durability = slot.Durability,
                    slotIndex = i
                });
            }
            structureInstance.Extra = JsonUtility.ToJson(data);
        }

        [System.Serializable]
        private sealed class StorageContentsData
        {
            public List<InventorySlotData> slots = new List<InventorySlotData>();
        }
    }
}
