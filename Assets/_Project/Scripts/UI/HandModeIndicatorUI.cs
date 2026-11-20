using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 화면 왼쪽 아래 손 모드 표시: 글자(사람 손/짐승 손/전환 중...)와 색 칸
    /// (docs/design/GAME_DESIGN.md 9.5). 그림이 나오기 전까지의 1단계 수준 단순 UI.
    /// </summary>
    public sealed class HandModeIndicatorUI : MonoBehaviour
    {
        private const string HumanLabel = "사람 손";
        private const string BeastLabel = "짐승 손";
        private const string TransitioningLabel = "전환 중...";

        [SerializeField] private PlayerHands hands;
        [SerializeField] private PlayerHandSettings settings;
        [SerializeField] private Text label;
        [SerializeField] private Image colorSwatch;

        private void Awake()
        {
            if (hands == null) Debug.LogError("[HandModeIndicatorUI] hands 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[HandModeIndicatorUI] settings 참조가 비어 있습니다.", this);
            if (label == null) Debug.LogError("[HandModeIndicatorUI] label 참조가 비어 있습니다.", this);
            if (colorSwatch == null) Debug.LogError("[HandModeIndicatorUI] colorSwatch 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (hands != null) hands.HandModeChanged += HandleHandModeChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (hands != null) hands.HandModeChanged -= HandleHandModeChanged;
        }

        private void Update() => Refresh();

        private void HandleHandModeChanged(HandMode mode) => Refresh();

        private void Refresh()
        {
            if (hands == null || settings == null || label == null) return;

            if (hands.IsTransitioning)
            {
                label.text = TransitioningLabel;
                if (colorSwatch != null) colorSwatch.color = settings.TransitioningColor;
                return;
            }

            bool beast = hands.Mode == HandMode.Beast;
            label.text = beast ? BeastLabel : HumanLabel;
            if (colorSwatch != null) colorSwatch.color = beast ? settings.BeastColor : settings.HumanColor;
        }
    }
}
