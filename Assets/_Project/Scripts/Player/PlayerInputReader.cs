using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Controls.inputactions를 읽어 이동·점프·달리기·손 전환·상호작용 입력을 내보낸다. 게임플레이
    /// 코드는 Input System을 직접 만지지 않고 이 컴포넌트가 주는 값만 쓴다 (ARCHITECTURE.md 2.2, 4.2).
    /// 맵 이동 중에는 SetLocked(true)로 모든 입력을 멈춘다 (ARCHITECTURE.md 3.3).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputAction moveAction;
        private InputAction runAction;
        private InputAction jumpAction;
        private InputAction switchHandAction;
        private InputAction interactAction;

        private bool locked;

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action SwitchHandPressed;
        public event Action InteractPressed;

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
        }

        private void OnEnable()
        {
            if (moveAction == null) return;
            moveAction.Enable();
            runAction.Enable();
            jumpAction.Enable();
            switchHandAction.Enable();
            interactAction.Enable();

            jumpAction.performed += HandleJumpPerformed;
            jumpAction.canceled += HandleJumpCanceled;
            switchHandAction.performed += HandleSwitchHandPerformed;
            interactAction.performed += HandleInteractPerformed;
        }

        private void OnDisable()
        {
            if (moveAction == null) return;
            jumpAction.performed -= HandleJumpPerformed;
            jumpAction.canceled -= HandleJumpCanceled;
            switchHandAction.performed -= HandleSwitchHandPerformed;
            interactAction.performed -= HandleInteractPerformed;

            moveAction.Disable();
            runAction.Disable();
            jumpAction.Disable();
            switchHandAction.Disable();
            interactAction.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;
            MoveInput = locked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            RunHeld = !locked && runAction.IsPressed();
        }

        /// <summary>맵 이동(페이드) 중 입력을 잠그거나 푼다. 잠기면 눌려 있던 버튼 이벤트도 무시한다.</summary>
        public void SetLocked(bool value)
        {
            locked = value;
            if (locked)
            {
                MoveInput = Vector2.zero;
                RunHeld = false;
            }
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
    }
}
