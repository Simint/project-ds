using UnityEngine;

namespace Game.Player
{
    /// <summary>상호작용 탐색 범위 (ARCHITECTURE.md 4.2). 밸런스 값이라 코드에 두지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Interaction Settings")]
    public sealed class PlayerInteractionSettings : ScriptableObject
    {
        [SerializeField] private float interactRadius = 0f;

        public float InteractRadius => interactRadius;
    }
}
