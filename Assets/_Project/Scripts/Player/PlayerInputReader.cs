using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Controls.inputactions를 읽어 이동·상호작용 입력을 내보낸다. 게임플레이 코드는
    /// Input System을 직접 만지지 않고 이 컴포넌트가 주는 값만 쓴다 (ARCHITECTURE.md 2.2, 4.2).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputAction moveAction;
        private InputAction interactAction;

        public Vector2 MoveInput { get; private set; }

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
            interactAction = map.FindAction("Interact", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            if (moveAction == null) return;
            moveAction.Enable();
            interactAction.Enable();
            interactAction.performed += HandleInteractPerformed;
        }

        private void OnDisable()
        {
            if (moveAction == null) return;
            interactAction.performed -= HandleInteractPerformed;
            moveAction.Disable();
            interactAction.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;
            MoveInput = moveAction.ReadValue<Vector2>();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }
    }
}
