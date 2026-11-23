using System.Collections.Generic;
using Game.Core;
using Game.Crafting;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 작업대 제작·수리 창 (ARCHITECTURE.md 4.4, docs/design/GAME_DESIGN.md 8). 재료가 모자란 항목은
    /// 버튼을 비활성화해 흐리게 보여준다(Unity Selectable의 기본 비활성 틴트를 그대로 쓴다). 수리
    /// 레시피는 가방에서 내구도가 닳은 같은 도구를 찾아 그 슬롯만 고쳐준다.
    /// </summary>
    public sealed class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Text entryTemplate;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private RecipeDatabase recipeDatabase;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHands hands;

        private readonly List<Text> spawnedEntries = new List<Text>();
        private StationKind currentStation;

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[CraftingUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[CraftingUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[CraftingUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);
            if (playerInventory == null) Debug.LogError("[CraftingUI] playerInventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[CraftingUI] itemDatabase 참조가 비어 있습니다.", this);
            if (recipeDatabase == null) Debug.LogError("[CraftingUI] recipeDatabase 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[CraftingUI] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[CraftingUI] hands 참조가 비어 있습니다.", this);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.UICancelPressed += HandleCancelPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.UICancelPressed -= HandleCancelPressed;
        }

        public void Show(StationKind station)
        {
            if (panelRoot == null) return;
            currentStation = station;
            Refresh();
            panelRoot.SetActive(true);
            inputReader?.SetLocked(true);
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            inputReader?.SetLocked(false);
        }

        private void HandleCancelPressed()
        {
            if (panelRoot != null && panelRoot.activeSelf) Hide();
        }

        private void Refresh()
        {
            foreach (Text entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();

            if (recipeDatabase == null) return;
            var materials = new MaterialSourceAggregator(playerInventory, GameServices.StorageRegistry);

            foreach (RecipeDefinition recipe in recipeDatabase.Recipes)
            {
                if (recipe == null || recipe.Station != currentStation) continue;

                bool handAllowed = hands == null || hands.AllowsHand(recipe.RequiredHand);
                bool canCraft = handAllowed && (recipe.IsRepair
                    ? FindDamagedToolSlot(recipe.ResultItemId) >= 0 && materials.HasAllIngredients(recipe.Ingredients)
                    : materials.HasAllIngredients(recipe.Ingredients));

                Text entry = Instantiate(entryTemplate, listParent);
                entry.gameObject.SetActive(true);
                entry.text = BuildLabel(recipe);

                Button button = entry.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = canCraft;
                    RecipeDefinition captured = recipe;
                    button.onClick.AddListener(() => Craft(captured));
                }

                spawnedEntries.Add(entry);
            }
        }

        private string BuildLabel(RecipeDefinition recipe)
        {
            string name = recipe.DisplayName;
            if (recipe.IsRepair) return $"{name} 수리";

            ItemDefinition resultDef = itemDatabase != null ? itemDatabase.Find(recipe.ResultItemId) : null;
            string resultName = resultDef != null ? resultDef.DisplayName : recipe.ResultItemId;
            return $"{resultName} x{recipe.ResultCount}";
        }

        private void Craft(RecipeDefinition recipe)
        {
            if (hands != null && !hands.AllowsHand(recipe.RequiredHand)) return;

            var materials = new MaterialSourceAggregator(playerInventory, GameServices.StorageRegistry);

            if (recipe.IsRepair)
            {
                int slotIndex = FindDamagedToolSlot(recipe.ResultItemId);
                ItemDefinition toolDef = itemDatabase != null ? itemDatabase.Find(recipe.ResultItemId) : null;
                if (slotIndex < 0 || toolDef == null || !toolDef.IsTool) return;
                if (!materials.TryConsume(recipe.Ingredients)) return;
                playerInventory.Model.RepairTool(slotIndex, toolDef.Tool.MaxDurability);
            }
            else
            {
                if (!materials.TryConsume(recipe.Ingredients)) return;
                playerInventory.Collect(recipe.ResultItemId, recipe.ResultCount);
            }

            Refresh();
        }

        private int FindDamagedToolSlot(string itemId)
        {
            if (playerInventory == null || itemDatabase == null) return -1;
            ItemDefinition def = itemDatabase.Find(itemId);
            if (def == null || !def.IsTool) return -1;

            IReadOnlyList<InventorySlot> slots = playerInventory.Model.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && !slot.IsEmpty && slot.ItemId == itemId && slot.Durability >= 0 && slot.Durability < def.Tool.MaxDurability)
                    return i;
            }
            return -1;
        }
    }
}
