using UnityEngine;

namespace Game.Player
{
    /// <summary>손 전환 수치 (docs/design/GAME_DESIGN.md 3.1). 밸런스 값이라 코드에 두지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Hand Settings")]
    public sealed class PlayerHandSettings : ScriptableObject
    {
        [SerializeField] private float transitionSeconds = 0.25f;

        public float TransitionSeconds => transitionSeconds;
    }
}
