using Game.Core;
using Game.Player;
using Game.Survival;
using Game.UI;
using UnityEngine;

namespace Game.Building
{
    /// <summary>침대 (ARCHITECTURE.md 4.5, docs/design/GAME_DESIGN.md 8). 아무 때나 E로 잘 시간을
    /// 고르는 창을 연다. 실제 시간 건너뛰기·스탯 모디파이어는 SleepUI가 맡는다.</summary>
    public sealed class Bed : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactPrompt = "잠자기";

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        public void Interact(GameObject interactor)
        {
            SleepUI ui = GameServices.SleepUI;
            PlayerSurvival survival = interactor != null ? interactor.GetComponentInParent<PlayerSurvival>() : null;
            if (ui == null || survival == null)
            {
                GameLog.Warning("Bed", "GameServices.SleepUI 또는 PlayerSurvival을 찾지 못했습니다.", this);
                return;
            }
            PlayerStatusEffects statusEffects = interactor.GetComponentInParent<PlayerStatusEffects>();
            if (statusEffects == null)
                GameLog.Warning("Bed", "PlayerStatusEffects를 찾지 못해 출혈·골절 확인 없이 잠 창을 엽니다.", this);
            ui.Show(survival, statusEffects);
        }
    }
}
