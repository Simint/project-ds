using Game.Inventory;
using Game.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 화면 아래 단축칸 1~0 표시 (docs/design/GAME_DESIGN.md 9.5 HUD 배치). 가방 앞 슬롯 10개를
    /// 그대로 보여주기만 한다 — 아직 쓸 수 있는 아이템이 없어 선택·사용 조작은 없다.
    /// </summary>
    public sealed class HotbarUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Text[] slotContentLabels = System.Array.Empty<Text>();

        private void Awake()
        {
            if (inventory == null) Debug.LogError("[HotbarUI] inventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[HotbarUI] itemDatabase 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Model.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Model.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (inventory == null) return;
            for (int i = 0; i < slotContentLabels.Length; i++)
            {
                if (slotContentLabels[i] == null) continue;
                if (i >= inventory.Model.Slots.Count || inventory.Model.Slots[i] == null || inventory.Model.Slots[i].IsEmpty)
                {
                    slotContentLabels[i].text = "";
                    continue;
                }

                InventorySlot slot = inventory.Model.Slots[i];
                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(slot.ItemId) : null;
                string name = def != null ? def.DisplayName : slot.ItemId;
                slotContentLabels[i].text = $"{name}\nx{slot.Count}";
            }
        }
    }
}
