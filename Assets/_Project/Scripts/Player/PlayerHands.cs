using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 손 모드(사람/짐승) 전환. 전환 중에는 모든 상호작용·공격을 막는다 (ARCHITECTURE.md 4.2,
    /// docs/design/GAME_DESIGN.md 3.1). 허기 배율 등 생존 스탯 연결은 HandModeChanged를 구독해서 한다
    /// (4단계에서 연결, 지금은 이벤트만 둔다).
    /// </summary>
    public sealed class PlayerHands : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHandSettings settings;
        [SerializeField] private SpriteRenderer bodyRenderer;

        private float transitionRemainingSeconds;

        public HandMode Mode { get; private set; } = HandMode.Human;
        public bool IsTransitioning => transitionRemainingSeconds > 0f;

        /// <summary>손 모드가 바뀔 때마다 (전환 시작 시점에) 발행한다.</summary>
        public event Action<HandMode> HandModeChanged;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[PlayerHands] inputReader 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerHands] settings 참조가 비어 있습니다.", this);
            if (bodyRenderer == null) Debug.LogError("[PlayerHands] bodyRenderer 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.SwitchHandPressed += HandleSwitchHandPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.SwitchHandPressed -= HandleSwitchHandPressed;
        }

        private void Update()
        {
            if (transitionRemainingSeconds > 0f) transitionRemainingSeconds -= Time.deltaTime;
            RefreshBodyColor();
        }

        /// <summary>전환 중에는 Any를 포함해 어떤 행동도 허용하지 않는다.</summary>
        public bool AllowsHand(HandRequirement requirement)
        {
            if (IsTransitioning) return false;
            return requirement == HandRequirement.Any
                   || (requirement == HandRequirement.Human && Mode == HandMode.Human)
                   || (requirement == HandRequirement.Beast && Mode == HandMode.Beast);
        }

        private void HandleSwitchHandPressed()
        {
            if (IsTransitioning) return;
            Mode = Mode == HandMode.Human ? HandMode.Beast : HandMode.Human;
            transitionRemainingSeconds = settings != null ? settings.TransitionSeconds : 0f;
            HandModeChanged?.Invoke(Mode);
        }

        /// <summary>그림이 나오기 전까지 플레이어 임시 사각형 색을 손 모드에 맞춘다 (docs/design 9.5).</summary>
        private void RefreshBodyColor()
        {
            if (bodyRenderer == null || settings == null) return;
            bodyRenderer.color = IsTransitioning
                ? settings.TransitioningColor
                : (Mode == HandMode.Beast ? settings.BeastColor : settings.HumanColor);
        }
    }
}
