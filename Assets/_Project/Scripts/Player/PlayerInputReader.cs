using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Controls.inputactions를 읽어 이동·점프·달리기·손 전환·상호작용·메뉴 입력을 내보낸다. 게임플레이
    /// 코드는 Input System을 직접 만지지 않고 이 컴포넌트가 주는 값만 쓴다 (ARCHITECTURE.md 2.2, 4.2).
    /// 맵 이동·지역 선택 창이 열린 동안은 SetLocked(true)로 이동·점프·손 전환·상호작용을 멈춘다
    /// (ARCHITECTURE.md 3.3). UI 탐색(UINavigate/UISubmit/UICancel)은 메뉴 전용이라 잠금의 영향을 받지
    /// 않는다 (잠긴 동안에도 메뉴를 조작할 수 있어야 하므로).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputAction moveAction;
        private InputAction runAction;
        private InputAction jumpAction;
        private InputAction switchHandAction;
        private InputAction interactAction;
        private InputAction uiNavigateAction;
        private InputAction uiSubmitAction;
        private InputAction uiCancelAction;

        private bool locked;

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }
        public float UINavigateInput { get; private set; }
        public bool IsLocked => locked;

        /// <summary>마지막으로 입력을 보낸 장치의 컨트롤 스킴 ("Keyboard&Mouse" 또는 "Gamepad"). 상호작용
        /// 안내의 키 표시를 여기에 맞춘다.</summary>
        public string ActiveControlScheme { get; private set; } = "Keyboard&Mouse";

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action SwitchHandPressed;
        public event Action InteractPressed;
        public event Action UISubmitPressed;
        public event Action UICancelPressed;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("[PlayerInputReader] actions 참조가 비어 있습니다.", this);
                return;
            }
            InputActionMap map = actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = map.FindAction("Move", throwIfNotFound: true);
            runAction = map.FindAction("Run", throwIfNotFound: true);
            jumpAction = map.FindAction("Jump", throwIfNotFound: true);
            switchHandAction = map.FindAction("SwitchHand", throwIfNotFound: true);
            interactAction = map.FindAction("Interact", throwIfNotFound: true);
            uiNavigateAction = map.FindAction("UINavigate", throwIfNotFound: true);
            uiSubmitAction = map.FindAction("UISubmit", throwIfNotFound: true);
            uiCancelAction = map.FindAction("UICancel", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            if (moveAction == null) return;
            moveAction.Enable();
            runAction.Enable();
            jumpAction.Enable();
            switchHandAction.Enable();
            interactAction.Enable();
            uiNavigateAction.Enable();
            uiSubmitAction.Enable();
            uiCancelAction.Enable();

            jumpAction.performed += HandleJumpPerformed;
            jumpAction.canceled += HandleJumpCanceled;
            switchHandAction.performed += HandleSwitchHandPerformed;
            interactAction.performed += HandleInteractPerformed;
            uiSubmitAction.performed += HandleUISubmitPerformed;
            uiCancelAction.performed += HandleUICancelPerformed;
        }

        private void OnDisable()
        {
            if (moveAction == null) return;
            jumpAction.performed -= HandleJumpPerformed;
            jumpAction.canceled -= HandleJumpCanceled;
            switchHandAction.performed -= HandleSwitchHandPerformed;
            interactAction.performed -= HandleInteractPerformed;
            uiSubmitAction.performed -= HandleUISubmitPerformed;
            uiCancelAction.performed -= HandleUICancelPerformed;

            moveAction.Disable();
            runAction.Disable();
            jumpAction.Disable();
            switchHandAction.Disable();
            interactAction.Disable();
            uiNavigateAction.Disable();
            uiSubmitAction.Disable();
            uiCancelAction.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;
            MoveInput = locked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            RunHeld = !locked && runAction.IsPressed();
            UINavigateInput = uiNavigateAction.ReadValue<float>();

            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame) ActiveControlScheme = "Gamepad";
            else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) ActiveControlScheme = "Keyboard&Mouse";
        }

        /// <summary>맵 이동(페이드)·지역 선택 창이 열린 동안 이동·점프·손 전환·상호작용을 멈춘다.</summary>
        public void SetLocked(bool value)
        {
            locked = value;
            if (locked)
            {
                MoveInput = Vector2.zero;
                RunHeld = false;
            }
        }

        /// <summary>현재 장치에서 상호작용 키의 표시 문자열 ("E", "X" 등). 안내 UI가 쓴다.</summary>
        public string GetInteractBindingDisplay()
        {
            if (interactAction == null) return "E";
            return interactAction.GetBindingDisplayString(default, ActiveControlScheme);
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            if (!locked) JumpPressed?.Invoke();
        }

        private void HandleJumpCanceled(InputAction.CallbackContext context)
        {
            if (!locked) JumpReleased?.Invoke();
        }

        private void HandleSwitchHandPerformed(InputAction.CallbackContext context)
        {
            if (!locked) SwitchHandPressed?.Invoke();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            if (!locked) InteractPressed?.Invoke();
        }

        private void HandleUISubmitPerformed(InputAction.CallbackContext context)
        {
            UISubmitPressed?.Invoke();
        }

        private void HandleUICancelPerformed(InputAction.CallbackContext context)
        {
            UICancelPressed?.Invoke();
        }
    }
}
