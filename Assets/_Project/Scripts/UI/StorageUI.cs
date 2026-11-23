using System.Collections.Generic;
using Game.Building;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 보관함 창 (docs/design/GAME_DESIGN.md 8, MVP 5단계). 항목을 누르면 가방으로 한 칸 꺼내고,
    /// "가방째 넣기" 버튼으로 가방 전체를 보관함에 옮긴다(칸이 모자라면 남는 만큼만). Boot 씬에 하나 두고
    /// GameBootstrap이 등록한다 — InventoryUI와 같은 구조다.
    /// </summary>
    public sealed class StorageUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Text entryTemplate;
        [SerializeField] private Button depositAllButton;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private PlayerInputReader inputReader;

        private readonly List<Text> spawnedEntries = new List<Text>();
        private StorageContainer currentStorage;
        private int openedFrame = -1;

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[StorageUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[StorageUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[StorageUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);
            if (depositAllButton == null) Debug.LogError("[StorageUI] depositAllButton 참조가 비어 있습니다.", this);
            if (playerInventory == null) Debug.LogError("[StorageUI] playerInventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[StorageUI] itemDatabase 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[StorageUI] inputReader 참조가 비어 있습니다.", this);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.UICancelPressed += HandleCancelPressed;
            if (depositAllButton != null) depositAllButton.onClick.AddListener(HandleDepositAllClicked);
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.UICancelPressed -= HandleCancelPressed;
                inputReader.SetLocked(this, false); // 열려 있는 채로 비활성화돼도 잠금이 영원히 남지 않게 한다
            }
            if (depositAllButton != null) depositAllButton.onClick.RemoveListener(HandleDepositAllClicked);
            if (currentStorage != null) currentStorage.Model.Changed -= Refresh;
        }

        public void Show(StorageContainer storage)
        {
            if (panelRoot == null || storage == null) return;
            if (currentStorage != null) currentStorage.Model.Changed -= Refresh;
            currentStorage = storage;
            currentStorage.Model.Changed += Refresh;
            Refresh();
            panelRoot.SetActive(true);
            openedFrame = Time.frameCount;
            inputReader?.SetLocked(this, true);
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (currentStorage != null) currentStorage.Model.Changed -= Refresh;
            currentStorage = null;
            inputReader?.SetLocked(this, false);
        }

        private void HandleCancelPressed()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            // Interact(패드 East)와 같은 물리 버튼이라, 보관함을 연 바로 그 입력에 바로 닫히는 걸 막는다
            // (TravelMenuUI와 같은 이유).
            if (Time.frameCount == openedFrame) return;
            Hide();
        }

        private void Refresh()
        {
            foreach (Text entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();

            if (currentStorage == null) return;
            IReadOnlyList<InventorySlot> slots = currentStorage.Model.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty) continue;

                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(slot.ItemId) : null;
                string name = def != null ? def.DisplayName : slot.ItemId;

                Text entry = Instantiate(entryTemplate, listParent);
                entry.gameObject.SetActive(true);
                entry.text = $"{name} x{slot.Count}";

                int slotIndex = i;
                Button button = entry.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => WithdrawSlot(slotIndex));

                spawnedEntries.Add(entry);
            }
        }

        private void WithdrawSlot(int slotIndex)
        {
            if (currentStorage == null) return;
            IReadOnlyList<InventorySlot> slots = currentStorage.Model.Slots;
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.IsEmpty) return;

            string itemId = slot.ItemId;
            long spoilAtMinute = slot.SpoilAtMinute;
            int durability = slot.Durability;
            int taken = currentStorage.Model.RemoveFromSlot(slotIndex, slot.Count);
            if (taken > 0) playerInventory.CollectWithState(itemId, taken, spoilAtMinute, durability);
        }

        /// <summary>가방의 모든 슬롯을 돌며 보관함에 들어가는 만큼만 옮긴다. 부패 시각·내구도도 그대로 옮긴다.</summary>
        private void HandleDepositAllClicked()
        {
            if (currentStorage == null || playerInventory == null) return;

            IReadOnlyList<InventorySlot> bagSlots = playerInventory.Model.Slots;
            var snapshot = new List<(int slotIndex, string itemId, int count, long spoilAtMinute, int durability)>();
            for (int i = 0; i < bagSlots.Count; i++)
            {
                InventorySlot slot = bagSlots[i];
                if (slot == null || slot.IsEmpty) continue;
                snapshot.Add((i, slot.ItemId, slot.Count, slot.SpoilAtMinute, slot.Durability));
            }

            foreach ((int slotIndex, string itemId, int count, long spoilAtMinute, int durability) in snapshot)
            {
                ItemDefinition def = itemDatabase != null ? itemDatabase.Find(itemId) : null;
                if (def == null) continue;
                int leftover = currentStorage.Model.TryAdd(itemId, count, def.EffectiveMaxStackSize, def.Weight, spoilAtMinute, durability);
                int moved = count - leftover;
                if (moved > 0) playerInventory.Model.RemoveFromSlot(slotIndex, moved);
            }
        }
    }
}
