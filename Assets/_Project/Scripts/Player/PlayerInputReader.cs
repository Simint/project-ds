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
        private InputAction toggleInventoryAction;
        private InputAction[] hotbarSlotActions;
        private InputAction hotbarNextAction;
        private InputAction hotbarPrevAction;
        private InputAction hotbarScrollAction;
        private InputAction useItemAction;

        // hotbarSlotActions마다 OnEnable/OnDisable에서 같은 델리게이트로 구독·해제해야 해서
        // (람다를 매번 새로 만들면 해제할 수 없다, .claude/rules/csharp.md) Awake에서 한 번 만들어 둔다.
        private Action<InputAction.CallbackContext>[] hotbarSlotHandlers;

        private bool locked;

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }
        public bool InteractHeld { get; private set; }
        public float UINavigateInput { get; private set; }

        /// <summary>마우스 휠 세로 값. 한 틱(한 칸)만큼만 오르내리고 다음 프레임에 0으로 돌아온다.</summary>
        public float HotbarScrollInput { get; private set; }
        public bool IsLocked => locked;

        /// <summary>마지막으로 입력을 보낸 장치의 컨트롤 스킴 ("Keyboard&Mouse" 또는 "Gamepad"). 상호작용
        /// 안내의 키 표시를 여기에 맞춘다.</summary>
        public string ActiveControlScheme { get; private set; } = "Keyboard&Mouse";

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action SwitchHandPressed;
        public event Action InteractPressed;
        public event Action InteractReleased;
        public event Action UISubmitPressed;
        public event Action UICancelPressed;
        public event Action ToggleInventoryPressed;

        /// <summary>단축칸 1~9,0을 직접 골랐을 때 (인덱스 0~9, '0' 키는 인덱스 9).</summary>
        public event Action<int> HotbarSlotSelected;
        public event Action HotbarNextPressed;
        public event Action HotbarPrevPressed;

        /// <summary>단축칸 선택 아이템 사용 (마우스 우클릭/F/패드 오른쪽 트리거, docs/design/GAME_DESIGN.md 3.1).</summary>
        public event Action UseItemPressed;

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
            toggleInventoryAction = map.FindAction("ToggleInventory", throwIfNotFound: true);
            hotbarNextAction = map.FindAction("HotbarNext", throwIfNotFound: true);
            hotbarPrevAction = map.FindAction("HotbarPrev", throwIfNotFound: true);
            hotbarScrollAction = map.FindAction("HotbarScroll", throwIfNotFound: true);
            useItemAction = map.FindAction("UseItem", throwIfNotFound: true);

            hotbarSlotActions = new InputAction[10];
            hotbarSlotHandlers = new Action<InputAction.CallbackContext>[10];
            for (int i = 0; i < hotbarSlotActions.Length; i++)
            {
                string actionName = i == 9 ? "Hotbar0" : $"Hotbar{i + 1}";
                hotbarSlotActions[i] = map.FindAction(actionName, throwIfNotFound: true);
                int index = i;
                hotbarSlotHandlers[i] = _ => { if (!locked) HotbarSlotSelected?.Invoke(index); };
            }
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
            toggleInventoryAction.Enable();
            hotbarNextAction.Enable();
            hotbarPrevAction.Enable();
            hotbarScrollAction.Enable();
            useItemAction.Enable();

            jumpAction.performed += HandleJumpPerformed;
            jumpAction.canceled += HandleJumpCanceled;
            switchHandAction.performed += HandleSwitchHandPerformed;
            interactAction.performed += HandleInteractPerformed;
            interactAction.canceled += HandleInteractCanceled;
            uiSubmitAction.performed += HandleUISubmitPerformed;
            uiCancelAction.performed += HandleUICancelPerformed;
            toggleInventoryAction.performed += HandleToggleInventoryPerformed;
            hotbarNextAction.performed += HandleHotbarNextPerformed;
            hotbarPrevAction.performed += HandleHotbarPrevPerformed;
            useItemAction.performed += HandleUseItemPerformed;

            for (int i = 0; i < hotbarSlotActions.Length; i++)
            {
                hotbarSlotActions[i].Enable();
                hotbarSlotActions[i].performed += hotbarSlotHandlers[i];
            }
        }

        private void OnDisable()
        {
            if (moveAction == null) return;
            jumpAction.performed -= HandleJumpPerformed;
            jumpAction.canceled -= HandleJumpCanceled;
            switchHandAction.performed -= HandleSwitchHandPerformed;
            interactAction.performed -= HandleInteractPerformed;
            interactAction.canceled -= HandleInteractCanceled;
            uiSubmitAction.performed -= HandleUISubmitPerformed;
            uiCancelAction.performed -= HandleUICancelPerformed;
            toggleInventoryAction.performed -= HandleToggleInventoryPerformed;
            hotbarNextAction.performed -= HandleHotbarNextPerformed;
            hotbarPrevAction.performed -= HandleHotbarPrevPerformed;
            useItemAction.performed -= HandleUseItemPerformed;

            for (int i = 0; i < hotbarSlotActions.Length; i++)
            {
                hotbarSlotActions[i].performed -= hotbarSlotHandlers[i];
                hotbarSlotActions[i].Disable();
            }

            moveAction.Disable();
            runAction.Disable();
            jumpAction.Disable();
            switchHandAction.Disable();
            interactAction.Disable();
            uiNavigateAction.Disable();
            uiSubmitAction.Disable();
            uiCancelAction.Disable();
            toggleInventoryAction.Disable();
            hotbarNextAction.Disable();
            hotbarPrevAction.Disable();
            hotbarScrollAction.Disable();
            useItemAction.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;
            MoveInput = locked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            RunHeld = !locked && runAction.IsPressed();
            InteractHeld = !locked && interactAction.IsPressed();
            UINavigateInput = uiNavigateAction.ReadValue<float>();
            HotbarScrollInput = locked ? 0f : hotbarScrollAction.ReadValue<float>();

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
                InteractHeld = false;
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

        private void HandleInteractCanceled(InputAction.CallbackContext context)
        {
            if (!locked) InteractReleased?.Invoke();
        }

        private void HandleToggleInventoryPerformed(InputAction.CallbackContext context)
        {
            if (!locked) ToggleInventoryPressed?.Invoke();
        }

        private void HandleHotbarNextPerformed(InputAction.CallbackContext context)
        {
            if (!locked) HotbarNextPressed?.Invoke();
        }

        private void HandleHotbarPrevPerformed(InputAction.CallbackContext context)
        {
            if (!locked) HotbarPrevPressed?.Invoke();
        }

        /// <summary>가방(Tab)이 열려 있는 동안에도 아이템을 쓸 수 있어야 해서 locked의 영향을 받지 않는다
        /// (UISubmit/UICancel과 같은 예외, docs/design/GAME_DESIGN.md 7).</summary>
        private void HandleUseItemPerformed(InputAction.CallbackContext context)
        {
            UseItemPressed?.Invoke();
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
