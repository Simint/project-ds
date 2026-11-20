using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어가 상호작용할 수 있는 대상 (문, 채집물, 상자 등).</summary>
    public interface IInteractable
    {
        string InteractPrompt { get; }

        /// <summary>이 상호작용에 필요한 손 모드. 대부분은 Any (ARCHITECTURE.md 4.2).</summary>
        HandRequirement RequiredHand { get; }

        void Interact(GameObject interactor);
    }
}
