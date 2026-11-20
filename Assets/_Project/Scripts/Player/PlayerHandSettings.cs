using UnityEngine;

namespace Game.Player
{
    /// <summary>손 전환 수치 (docs/design/GAME_DESIGN.md 3.1). 밸런스 값이라 코드에 두지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Hand Settings")]
    public sealed class PlayerHandSettings : ScriptableObject
    {
        [SerializeField] private float transitionSeconds = 0.25f;

        [Header("임시 그림 색 (그림이 나오기 전까지)")]
        [SerializeField] private Color humanColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color beastColor = new Color(0.8f, 0.35f, 0.15f);
        [SerializeField] private Color transitioningColor = new Color(0.6f, 0.6f, 0.6f);

        public float TransitionSeconds => transitionSeconds;
        public Color HumanColor => humanColor;
        public Color BeastColor => beastColor;
        public Color TransitioningColor => transitioningColor;
    }
}
