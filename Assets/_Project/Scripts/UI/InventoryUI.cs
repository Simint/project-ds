using System.Collections.Generic;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 가방 창 (Tab / 패드 Select, docs/design/GAME_DESIGN.md 7). 슬롯 목록과 총 무게를 보여준다.
    /// 1단계 수준 단순 UI: 고르거나 옮기는 조작은 없다. 열려 있는 동안 플레이어 이동을 잠근다.
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
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleInventoryPressed -= HandleTogglePressed;
                inputReader.UICancelPressed -= HandleCancelPressed;
            }
        }

        private void HandleTogglePressed()
        {
            if (panelRoot != null && panelRoot.activeSelf) Hide();
            else Show();
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
            inputReader?.SetLocked(true);
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            inputReader?.SetLocked(false);
        }

        private void Refresh()
        {
            foreach (Text entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();

            foreach ((string itemId, int count) in inventory.EnumerateSlots())
            {
                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
                string name = def != null ? def.DisplayName : itemId;
                Text entry = Instantiate(entryTemplate, listParent);
                entry.gameObject.SetActive(true);
                entry.text = $"{name} x{count}";
                spawnedEntries.Add(entry);
            }

            if (weightText != null)
                weightText.text = $"무게: {inventory.Model.TotalWeight:F1} / {inventory.WeightLimit:F0}";
        }
    }
}
