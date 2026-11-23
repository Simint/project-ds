using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 화로 (ARCHITECTURE.md 4.5, docs/design/GAME_DESIGN.md 8). 장작(통나무)을 들고 쓰면 불을 붙이거나
    /// 더 태우고, 불이 켜진 동안 더러운 물병을 들고 쓰면 깨끗한 물병으로 끓여준다. 연료가 다 타는
    /// 시각은 StructureInstance.UntilMinute에 그대로 저장된다 — 다른 구조물처럼 저장·복원된다.
    /// </summary>
    public sealed class Stove : MonoBehaviour, IInteractable
    {
        [SerializeField] private StoveSettings settings;
        [SerializeField] private string interactPrompt = "화로 쓰기";

        private StructureInstance structureInstance;

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        public bool IsLit => structureInstance != null && GameServices.WorldClock != null
                              && structureInstance.UntilMinute > GameServices.WorldClock.TotalMinutes;

        private void Awake()
        {
            structureInstance = GetComponent<StructureInstance>();
            if (structureInstance == null) Debug.LogError("[Stove] StructureInstance가 없습니다.", this);
            if (settings == null) Debug.LogError("[Stove] settings 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (settings == null) return;
            GameServices.HeatZoneRegistry?.Register(transform, settings.HeatZoneRadiusTiles, settings.HeatZoneExtraRecoveryPerHour, () => IsLit);
        }

        private void OnDisable() => GameServices.HeatZoneRegistry?.Unregister(transform);

        public void Interact(GameObject interactor)
        {
            if (structureInstance == null || settings == null) return;
            PlayerHeldItem heldItem = interactor != null ? interactor.GetComponentInParent<PlayerHeldItem>() : null;
            ItemDefinition selected = heldItem?.SelectedDefinition;
            if (selected == null) return;

            if (selected.Id == settings.FuelItemId) AddFuel(heldItem);
            else if (selected.Id == WaterItemIds.DirtyWaterBottle && IsLit) BoilWater(heldItem);
        }

        private void AddFuel(PlayerHeldItem heldItem)
        {
            int taken = heldItem.Inventory.Model.RemoveFromSlot(heldItem.SelectedSlotIndex, 1);
            if (taken <= 0) return;

            long now = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            long baseline = System.Math.Max(structureInstance.UntilMinute, now);
            structureInstance.UntilMinute = baseline + settings.FuelMinutesPerWoodLog;
        }

        private void BoilWater(PlayerHeldItem heldItem)
        {
            int taken = heldItem.Inventory.Model.RemoveFromSlot(heldItem.SelectedSlotIndex, 1);
            if (taken > 0) heldItem.Inventory.Collect(WaterItemIds.CleanWaterBottle, 1);
        }
    }
}
