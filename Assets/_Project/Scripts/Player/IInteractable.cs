using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어가 상호작용할 수 있는 대상 (문, 채집물, 상자 등). 손 모드 제약은 2단계에서 추가한다.</summary>
    public interface IInteractable
    {
        string InteractPrompt { get; }

        void Interact(GameObject interactor);
    }
}
