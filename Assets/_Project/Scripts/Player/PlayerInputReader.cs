using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Controls.inputactions를 읽어 이동·점프·달리기·손 전환·상호작용·메뉴 입력을 내보낸다. 게임플레이
    /// 코드는 Input System을 직접 만지지 않고 이 컴포넌트가 주는 값만 쓴다 (ARCHITECTURE.md 2.2, 4.2).
    /// 맵 이동·지역 선택 창이 열린 동안은 SetLocked(source, true)로 이동·점프·손 전환·상호작용을 멈춘다
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
        private InputAction toggleBuildModeAction;
        private InputAction buildPlaceAction;
        private InputAction buildDemolishAction;
        private InputAction buildCursorMoveAction;
        private InputAction pointerPositionAction;

        private Camera mainCamera;

        // hotbarSlotActions마다 OnEnable/OnDisable에서 같은 델리게이트로 구독·해제해야 해서
        // (람다를 매번 새로 만들면 해제할 수 없다, .claude/rules/csharp.md) Awake에서 한 번 만들어 둔다.
        private Action<InputAction.CallbackContext>[] hotbarSlotHandlers;

        // 잠금을 하나의 bool로 두면, 두 UI가 동시에 잠그고 있을 때 한쪽이 풀면 다른 쪽 것까지 같이
        // 풀려 버린다(예: 건설 모드가 잠근 상태에서 상점 UI가 열려 잠그고 닫으면 건설 모드까지 풀림).
        // 잠근 쪽(this로 넘기는 호출자)마다 따로 기억하는 집합으로 바꿔 출처별로 독립시킨다.
        private readonly HashSet<object> lockSources = new HashSet<object>();

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }
        public bool InteractHeld { get; private set; }
        public float UINavigateInput { get; private set; }

        /// <summary>마우스 휠 세로 값. 한 틱(한 칸)만큼만 오르내리고 다음 프레임에 0으로 돌아온다.</summary>
        public float HotbarScrollInput { get; private set; }

        /// <summary>건설 모드에서 패드 왼쪽 스틱으로 커서를 움직일 때 쓴다. 잠금의 영향을 받지 않는다
        /// (건설 모드 자체가 잠금을 걸기 때문).</summary>
        public Vector2 BuildCursorMoveInput { get; private set; }

        /// <summary>마우스 포인터의 월드 좌표 (건설 모드 커서용). Keyboard&amp;Mouse가 아니면 의미 없다.</summary>
        public Vector2 PointerWorldPosition { get; private set; }

        public bool IsLocked => lockSources.Count > 0;

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

        /// <summary>건설 모드 켜고 끄기 (B/패드 Start, docs/design/GAME_DESIGN.md 3.1). 잠금과 무관하게
        /// 항상 발행한다 — 잠금을 거는 쪽이 건설 모드 자신이라 UICancel과 같은 예외다.</summary>
        public event Action ToggleBuildModePressed;

        /// <summary>건설 모드에서 설치(왼쪽 클릭/패드 버튼 South). 건설 모드가 아닐 때는 받는 쪽에서 무시한다.</summary>
        public event Action BuildPlacePressed;

        /// <summary>건설 모드에서 철거(오른쪽 클릭/패드 버튼 East).</summary>
        public event Action BuildDemolishPressed;

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
            toggleBuildModeAction = map.FindAction("ToggleBuildMode", throwIfNotFound: true);
            buildPlaceAction = map.FindAction("BuildPlace", throwIfNotFound: true);
            buildDemolishAction = map.FindAction("BuildDemolish", throwIfNotFound: true);
            buildCursorMoveAction = map.FindAction("BuildCursorMove", throwIfNotFound: true);
            pointerPositionAction = map.FindAction("PointerPosition", throwIfNotFound: true);
            mainCamera = Camera.main;

            hotbarSlotActions = new InputAction[10];
            hotbarSlotHandlers = new Action<InputAction.CallbackContext>[10];
            for (int i = 0; i < hotbarSlotActions.Length; i++)
            {
                string actionName = i == 9 ? "Hotbar0" : $"Hotbar{i + 1}";
                hotbarSlotActions[i] = map.FindAction(actionName, throwIfNotFound: true);
                int index = i;
                hotbarSlotHandlers[i] = _ => { if (!IsLocked) HotbarSlotSelected?.Invoke(index); };
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
            toggleBuildModeAction.Enable();
            buildPlaceAction.Enable();
            buildDemolishAction.Enable();
            buildCursorMoveAction.Enable();
            pointerPositionAction.Enable();

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
            toggleBuildModeAction.performed += HandleToggleBuildModePerformed;
            buildPlaceAction.performed += HandleBuildPlacePerformed;
            buildDemolishAction.performed += HandleBuildDemolishPerformed;

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
            toggleBuildModeAction.performed -= HandleToggleBuildModePerformed;
            buildPlaceAction.performed -= HandleBuildPlacePerformed;
            buildDemolishAction.performed -= HandleBuildDemolishPerformed;

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
            toggleBuildModeAction.Disable();
            buildPlaceAction.Disable();
            buildDemolishAction.Disable();
            buildCursorMoveAction.Disable();
            pointerPositionAction.Disable();
        }

        private void Update()
        {
            if (moveAction == null) return;
            bool isLocked = IsLocked;
            MoveInput = isLocked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            RunHeld = !isLocked && runAction.IsPressed();
            InteractHeld = !isLocked && interactAction.IsPressed();
            UINavigateInput = uiNavigateAction.ReadValue<float>();
            HotbarScrollInput = isLocked ? 0f : hotbarScrollAction.ReadValue<float>();
            BuildCursorMoveInput = buildCursorMoveAction.ReadValue<Vector2>();
            if (mainCamera != null)
            {
                Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
                Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
                PointerWorldPosition = new Vector2(world.x, world.y);
            }

            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame) ActiveControlScheme = "Gamepad";
            else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) ActiveControlScheme = "Keyboard&Mouse";
        }

        /// <summary>맵 이동(페이드)·지역 선택 창 등이 열린 동안 이동·점프·손 전환·상호작용을 멈춘다.
        /// source는 잠근 쪽 자신(보통 this)을 넘긴다 — 그래야 서로 다른 UI가 동시에 잠궈도 한쪽이
        /// 풀 때 다른 쪽 잠금까지 같이 풀리지 않는다.</summary>
        public void SetLocked(object source, bool value)
        {
            if (source == null) return;
            if (value) lockSources.Add(source);
            else lockSources.Remove(source);

            if (IsLocked)
            {
                MoveInput = Vector2.zero;
                RunHeld = false;
                InteractHeld = false;
            }
        }

        /// <summary>자기 자신(source)이 아닌 다른 출처가 지금 잠그고 있는지. Tab(가방)·B(건설 모드)처럼
        /// 스스로 잠금을 거는 토글 입력이, 자기 잠금 때문에 자기 토글이 막히지 않으면서도 다른 UI가 열려
        /// 있을 때는 거부할 수 있게 한다.</summary>
        public bool IsLockedByOthers(object source)
        {
            if (lockSources.Count == 0) return false;
            if (source == null) return true;
            foreach (object held in lockSources)
                if (held != source) return true;
            return false;
        }

        /// <summary>현재 장치에서 상호작용 키의 표시 문자열 ("E", "X" 등). 안내 UI가 쓴다.</summary>
        public string GetInteractBindingDisplay()
        {
            if (interactAction == null) return "E";
            return interactAction.GetBindingDisplayString(default, ActiveControlScheme);
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocked) JumpPressed?.Invoke();
        }

        private void HandleJumpCanceled(InputAction.CallbackContext context)
        {
            if (!IsLocked) JumpReleased?.Invoke();
        }

        private void HandleSwitchHandPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocked) SwitchHandPressed?.Invoke();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocked) InteractPressed?.Invoke();
        }

        private void HandleInteractCanceled(InputAction.CallbackContext context)
        {
            if (!IsLocked) InteractReleased?.Invoke();
        }

        /// <summary>가방 자신이 잠금을 걸기 때문에(Tab으로 열면 그 뒤로 잠겨서) ToggleBuildMode/UICancel과
        /// 같은 이유로 잠금과 무관하게 항상 발행한다 — 안 그러면 Tab으로 연 가방을 Tab으로 못 닫는다.
        /// 다른 UI가 잠근 동안 Tab으로 가방이 열리면 안 되는 건 받는 쪽(InventoryUI)이
        /// IsLockedByOthers로 직접 막는다.</summary>
        private void HandleToggleInventoryPerformed(InputAction.CallbackContext context)
        {
            ToggleInventoryPressed?.Invoke();
        }

        private void HandleHotbarNextPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocked) HotbarNextPressed?.Invoke();
        }

        private void HandleHotbarPrevPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocked) HotbarPrevPressed?.Invoke();
        }

        /// <summary>가방이 열려 있어도 아이템을 쓸 수 있어야 해서 잠금의 영향을 받지 않는다
        /// (UISubmit/UICancel과 같은 예외, docs/design/GAME_DESIGN.md 7).</summary>
        private void HandleUseItemPerformed(InputAction.CallbackContext context)
        {
            UseItemPressed?.Invoke();
        }

        /// <summary>건설 모드 자신이 잠금을 걸기 때문에 UICancel처럼 잠금과 무관하게 항상 발행한다.</summary>
        private void HandleToggleBuildModePerformed(InputAction.CallbackContext context)
        {
            ToggleBuildModePressed?.Invoke();
        }

        /// <summary>건설 모드가 아닐 때 눌러도 해롭지 않다 — 받는 쪽(BuildModeController)이 활성 여부를
        /// 직접 확인한다.</summary>
        private void HandleBuildPlacePerformed(InputAction.CallbackContext context)
        {
            BuildPlacePressed?.Invoke();
        }

        private void HandleBuildDemolishPerformed(InputAction.CallbackContext context)
        {
            BuildDemolishPressed?.Invoke();
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
