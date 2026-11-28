using System.Collections.Generic;
using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 가방 창 (Tab / 패드 Select, docs/design/GAME_DESIGN.md 7). 슬롯 목록과 총 무게를 보여주고,
    /// 소비 아이템 항목을 누르면 그 슬롯을 바로 쓴다(7.1 "가방에서도 쓸 수 있다"). 열려 있는 동안
    /// 플레이어 이동을 잠근다.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Text entryTemplate;
        [SerializeField] private Text weightText;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private ItemUseController itemUseController;

        private readonly List<Text> spawnedEntries = new List<Text>();

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[InventoryUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[InventoryUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[InventoryUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);
            if (weightText == null) Debug.LogError("[InventoryUI] weightText 참조가 비어 있습니다.", this);
            if (inventory == null) Debug.LogError("[InventoryUI] inventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[InventoryUI] itemDatabase 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[InventoryUI] inputReader 참조가 비어 있습니다.", this);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleInventoryPressed += HandleTogglePressed;
                inputReader.UICancelPressed += HandleCancelPressed;
            }
            if (inventory != null) inventory.Model.Changed += HandleModelChanged;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleInventoryPressed -= HandleTogglePressed;
                inputReader.UICancelPressed -= HandleCancelPressed;
                inputReader.SetLocked(this, false); // 열려 있는 채로 비활성화돼도 잠금이 영원히 남지 않게 한다
            }
            if (inventory != null) inventory.Model.Changed -= HandleModelChanged;
        }

        private void HandleModelChanged()
        {
            if (panelRoot != null && panelRoot.activeSelf) Refresh();
        }

        private void HandleTogglePressed()
        {
            if (panelRoot != null && panelRoot.activeSelf) { Hide(); return; }
            // 다른 UI(상점·제작대·건설 모드 등)가 이미 잠궈 둔 동안엔 Tab으로 가방까지 겹쳐 열지 않는다.
            if (inputReader != null && inputReader.IsLockedByOthers(this)) return;
            Show();
        }

        private void HandleCancelPressed()
        {
            if (panelRoot != null && panelRoot.activeSelf) Hide();
        }

        private void Show()
        {
            if (panelRoot == null || inventory == null) return;
            Refresh();
            panelRoot.SetActive(true);
            inputReader?.SetLocked(this, true, allowItemUse: true); // 가방을 연 채로 단축칸 아이템을 쓸 수 있다 (7장)
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            inputReader?.SetLocked(this, false);
        }

        private void Refresh()
        {
            foreach (Text entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();

            long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            IReadOnlyList<InventorySlot> slots = inventory.Model.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty) continue;

                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(slot.ItemId) : null;
                string name = def != null ? def.DisplayName : slot.ItemId;
                bool spoiled = slot.IsSpoiled(now);
                if (spoiled) name += " (상함)";

                Text entry = Instantiate(entryTemplate, listParent);
                entry.gameObject.SetActive(true);
                entry.text = $"{name} x{slot.Count}";
                entry.color = spoiled ? Color.red : Color.white;

                if (def != null && def.IsConsumable && itemUseController != null)
                {
                    int slotIndex = i;
                    Button button = entry.GetComponent<Button>();
                    if (button != null) button.onClick.AddListener(() => itemUseController.UseSlot(slotIndex));
                }

                spawnedEntries.Add(entry);
            }

            if (weightText != null)
                weightText.text = $"무게: {inventory.Model.TotalWeight:F1} / {inventory.WeightLimit:F0}";
        }
    }
}
