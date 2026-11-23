using Game.Core;
using Game.Crafting;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Building
{
    /// <summary>작업대 (ARCHITECTURE.md 4.5, docs/design/GAME_DESIGN.md 8). E로 제작·수리 창을 연다.
    /// 상태는 없다 — 보여줄 목록은 CraftingUI가 StationKind로 걸러서 직접 고른다.</summary>
    public sealed class Workbench : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactPrompt = "작업대 사용";

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Human;
        public float HoldSecondsRequired => 0f;

        public void Interact(GameObject interactor)
        {
            CraftingUI ui = GameServices.CraftingUI;
            if (ui == null)
            {
                GameLog.Warning("Workbench", "GameServices.CraftingUI가 등록되지 않았습니다.", this);
                return;
            }
            ui.Show(StationKind.Workbench);
        }
    }
}
