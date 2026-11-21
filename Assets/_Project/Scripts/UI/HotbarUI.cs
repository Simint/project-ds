using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 화면 아래 단축칸 1~0 표시 (docs/design/GAME_DESIGN.md 9.5 HUD 배치). 가방 앞 슬롯 10개를
    /// 보여주고, 상한 아이템은 빨갛게, 현재 선택된 칸은 이름 앞에 표시를 붙인다.
    /// </summary>
    public sealed class HotbarUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private HotbarSelector hotbar;
        [SerializeField] private Text[] slotContentLabels = System.Array.Empty<Text>();

        private void Awake()
        {
            if (inventory == null) Debug.LogError("[HotbarUI] inventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[HotbarUI] itemDatabase 참조가 비어 있습니다.", this);
            if (hotbar == null) Debug.LogError("[HotbarUI] hotbar 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Model.Changed += Refresh;
            if (hotbar != null) hotbar.SelectedIndexChanged += HandleSelectedIndexChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Model.Changed -= Refresh;
            if (hotbar != null) hotbar.SelectedIndexChanged -= HandleSelectedIndexChanged;
        }

        private void HandleSelectedIndexChanged(int _) => Refresh();

        private void Refresh()
        {
            if (inventory == null) return;
            long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;

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
                bool spoiled = slot.IsSpoiled(now);
                if (spoiled) name += " (상함)";
                if (hotbar != null && hotbar.SelectedIndex == i) name = "▶ " + name;

                slotContentLabels[i].text = $"{name}\nx{slot.Count}";
                slotContentLabels[i].color = spoiled ? Color.red : Color.white;
            }
        }
    }
}
