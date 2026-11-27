using UnityEngine;

namespace Game.Player
{
    /// <summary>손 전환 수치 (docs/design/GAME_DESIGN.md 3.1). 밸런스 값이라 코드에 두지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Hand Settings")]
    public sealed class PlayerHandSettings : ScriptableObject
    {
        [SerializeField] private float transitionSeconds = 0f;

        [Header("몸 그림 색조 (손 모드 그림이 나오기 전까지). 그림에 곱해지므로 흰색 = 원래 색")]
        [SerializeField] private Color humanColor = Color.white;
        [SerializeField] private Color beastColor = new Color(1f, 0.82f, 0.7f);
        [SerializeField] private Color transitioningColor = new Color(0.75f, 0.75f, 0.75f);

        public float TransitionSeconds => transitionSeconds;
        public Color HumanColor => humanColor;
        public Color BeastColor => beastColor;
        public Color TransitioningColor => transitioningColor;
    }
}
