using Game.Items;
using Game.Player;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 지금 단축칸에서 고른 아이템이 뭔지 알려준다 (ARCHITECTURE.md 4.2/4.4). 도구 조건 판정
    /// (Harvestable의 IToolGated)과 물 담기(WaterSource) 등 여러 곳에서 "지금 손에 든 게 뭔지" 물을 때
    /// 같은 창구를 쓰게 한다.
    /// </summary>
    public sealed class PlayerHeldItem : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private HotbarSelector hotbar;
        [SerializeField] private ItemDatabase itemDatabase;

        private void Awake()
        {
            if (inventory == null) Debug.LogError("[PlayerHeldItem] inventory 참조가 비어 있습니다.", this);
            if (hotbar == null) Debug.LogError("[PlayerHeldItem] hotbar 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[PlayerHeldItem] itemDatabase 참조가 비어 있습니다.", this);
        }

        public PlayerInventory Inventory => inventory;
        public int SelectedSlotIndex => hotbar != null ? hotbar.SelectedIndex : -1;

        public InventorySlot SelectedSlot
        {
            get
            {
                if (inventory == null || hotbar == null) return null;
                int index = hotbar.SelectedIndex;
                return index >= 0 && index < inventory.Model.Slots.Count ? inventory.Model.Slots[index] : null;
            }
        }

        public ItemDefinition SelectedDefinition
        {
            get
            {
                InventorySlot slot = SelectedSlot;
                if (slot == null || slot.IsEmpty || itemDatabase == null) return null;
                return itemDatabase.Find(slot.ItemId);
            }
        }
    }
}
