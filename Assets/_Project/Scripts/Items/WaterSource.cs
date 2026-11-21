using Game.Core;
using Game.Player;
using Game.Survival;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 주택가의 더러운 물웅덩이 (docs/design/GAME_DESIGN.md 5.5, MVP 4단계). E 한 번으로 바로 마신다 —
    /// 양이 줄지 않고 상태를 저장할 필요가 없어 IPersistentObject를 쓰지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WaterSource : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactPrompt = "마시기 (더러운 물)";
        [SerializeField] private bool isDirty = true;
        [SerializeField] private SurvivalSettings settings;

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        private void Awake()
        {
            if (settings == null) Debug.LogError("[WaterSource] settings 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (settings == null || interactor == null) return;

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
