using Game.Core;
using Game.Items;
using Game.Player;
using Game.Survival;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 단축칸 사용 키(또는 가방 클릭)로 소비 아이템 한 개를 쓴다 (ARCHITECTURE.md 4.4,
    /// docs/design/GAME_DESIGN.md 3.1, 5.5). 상한 음식·더러운 물의 lazy 판정도 여기서 한다 —
    /// 슬롯의 itemId는 그대로 두고, 쓰는 순간에만 spoilAtMinute과 현재 시각을 비교한다.
    /// </summary>
    public sealed class ItemUseController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private HotbarSelector hotbar;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerStatusEffects statusEffects;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[ItemUseController] inputReader 참조가 비어 있습니다.", this);
            if (hotbar == null) Debug.LogError("[ItemUseController] hotbar 참조가 비어 있습니다.", this);
            if (inventory == null) Debug.LogError("[ItemUseController] inventory 참조가 비어 있습니다.", this);
            if (itemDatabase == null) Debug.LogError("[ItemUseController] itemDatabase 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[ItemUseController] hands 참조가 비어 있습니다.", this);
            if (survival == null) Debug.LogError("[ItemUseController] survival 참조가 비어 있습니다.", this);
            if (statusEffects == null) Debug.LogError("[ItemUseController] statusEffects 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.UseItemPressed += HandleUseItemPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.UseItemPressed -= HandleUseItemPressed;
        }

        private void HandleUseItemPressed()
        {
            // 다른 창·맵 이동 중에는 PlayerInputReader가 UseItem을 아예 보내지 않는다(IsItemUseLocked, 가방 창만 예외).
            // 아래 맵 이동 확인은 그 잠금과 겹치는 중복 안전장치다(잠금 출처가 바뀌어도 이동 중 사용을 막는다).
            if (GameServices.MapLoader != null && GameServices.MapLoader.IsTraveling) return;
            // 마우스 오른쪽 버튼이 UseItem과 건설 모드의 BuildDemolish에 같이 묶여 있다 — 건설 모드 중엔
            // 철거만 하고, 손에 든 소비 아이템을 같이 쓰지 않는다.
            if (GameServices.BuildModeController != null && GameServices.BuildModeController.IsActive) return;
            if (hotbar != null) UseSlot(hotbar.SelectedIndex);
        }

        /// <summary>단축칸 사용 키와 가방 창 클릭이 같은 메서드를 쓴다 (docs/design/GAME_DESIGN.md 7).</summary>
        public void UseSlot(int slotIndex)
        {
            if (inventory == null || itemDatabase == null) return;
            if (slotIndex < 0 || slotIndex >= inventory.Model.Slots.Count) return;

            InventorySlot slot = inventory.Model.Slots[slotIndex];
            if (slot == null || slot.IsEmpty) return;

            ItemDefinition def = itemDatabase.Find(slot.ItemId);
            if (def == null || !def.IsConsumable) return;
            if (hands != null && !hands.AllowsHand(def.RequiredHand)) return;

            long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            bool spoiled = slot.IsSpoiled(now);
            ItemDefinition effectiveDef = def;
            if (spoiled && !string.IsNullOrEmpty(def.Consumable.SpoiledItemId))
                effectiveDef = itemDatabase.Find(def.Consumable.SpoiledItemId) ?? def;

            ApplyConsumable(effectiveDef);
            inventory.Model.RemoveFromSlot(slotIndex, 1);
        }

        private void ApplyConsumable(ItemDefinition def)
        {
            if (survival != null)
            {
                foreach (StatRestoreAmount restore in def.Consumable.StatRestores)
                    survival.Stats.Get(restore.StatId)?.Add(restore.Amount);
                survival.Stats.NotifyChanged();
            }

            if (statusEffects != null && !string.IsNullOrEmpty(def.Consumable.CuresStatusEffectId))
                statusEffects.Cure(def.Consumable.CuresStatusEffectId);

            if (statusEffects != null && def.Consumable.SicknessChance > 0f && Random.value < def.Consumable.SicknessChance)
                statusEffects.Apply(StatusEffectIds.Sickness);

            if (!string.IsNullOrEmpty(def.Consumable.LeavesContainerItemId))
                inventory.Collect(def.Consumable.LeavesContainerItemId, 1);
        }
    }
}
