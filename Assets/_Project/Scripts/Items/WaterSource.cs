using Game.Core;
using Game.Inventory;
using Game.Player;
using Game.Survival;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 주택가의 더러운 물웅덩이 (docs/design/GAME_DESIGN.md 5.5, 8). 빈 병을 들고 있으면 더러운
    /// 물병을 채우고, 아니면(기존 동작 유지) 바로 마신다. E 한 번, 양이 줄지 않고 상태를 저장할
    /// 필요가 없어 IPersistentObject를 쓰지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WaterSource : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactPrompt = "마시기 (더러운 물)";
        [SerializeField] private bool isDirty = true;
        [SerializeField] private SurvivalSettings settings;
        [Tooltip("바깥 물웅덩이는 겨울에 언다 (docs/design/GAME_DESIGN.md 5.6).")]
        [SerializeField] private bool freezesInWinter = true;
        [SerializeField] private string frozenPrompt = "얼어 있어 마실 수 없다";

        public string InteractPrompt => IsFrozen ? frozenPrompt : interactPrompt;

        /// <summary>실외 맵의 물웅덩이만 언다. 실내 물은 freezesInWinter를 끈다.</summary>
        private bool IsFrozen => freezesInWinter && GameServices.Seasons != null && GameServices.Seasons.IsWinter
                                 && !(GameServices.MapLoader != null && GameServices.MapLoader.CurrentMap != null && GameServices.MapLoader.CurrentMap.IsIndoor);
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        private void Awake()
        {
            if (settings == null) Debug.LogError("[WaterSource] settings 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (settings == null || interactor == null || IsFrozen) return;

            PlayerHeldItem heldItem = interactor.GetComponentInParent<PlayerHeldItem>();
            if (heldItem != null && heldItem.SelectedDefinition != null && heldItem.SelectedDefinition.Id == WaterItemIds.EmptyBottle)
            {
                FillBottle(heldItem);
                return;
            }

            DrinkDirectly(interactor);
        }

        private void FillBottle(PlayerHeldItem heldItem)
        {
            int taken = heldItem.Inventory.Model.RemoveFromSlot(heldItem.SelectedSlotIndex, 1);
            if (taken <= 0) return;
            heldItem.Inventory.Collect(isDirty ? WaterItemIds.DirtyWaterBottle : WaterItemIds.CleanWaterBottle, 1);
        }

        private void DrinkDirectly(GameObject interactor)
        {
            PlayerSurvival survival = interactor.GetComponentInParent<PlayerSurvival>();
            if (survival == null)
            {
                GameLog.Warning("WaterSource", "interactor에 PlayerSurvival이 없습니다.", this);
                return;
            }
            survival.Stats.Get(StatIds.Thirst)?.Add(settings.DirtyWaterThirstRestore);
            survival.Stats.NotifyChanged();

            if (isDirty && Random.value < settings.DirtyWaterSicknessChance)
            {
                PlayerStatusEffects statusEffects = interactor.GetComponentInParent<PlayerStatusEffects>();
                statusEffects?.Apply(StatusEffectIds.Sickness);
            }
        }
    }
}
