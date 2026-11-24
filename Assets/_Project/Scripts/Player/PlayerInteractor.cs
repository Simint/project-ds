using System.Collections.Generic;
using Game.Core;
using Game.Inventory;
using Game.UI;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 주변 Interactable 레이어에서 가장 가까운 IInteractable을 찾아 안내 UI에 띄우고, 상호작용
    /// 버튼을 누르면 실행한다 (ARCHITECTURE.md 4.2). HoldSecondsRequired가 0보다 크면 그만큼 눌러
    /// 담아야 완료된다(도구가 필요한 채집물 등, docs/design/GAME_DESIGN.md 7.1). 결과 버퍼는 미리
    /// 할당해 둔다.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private PlayerHeldItem heldItem;
        [SerializeField] private InteractPromptUI promptUI;
        [SerializeField] private PlayerInteractionSettings settings;

        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private readonly Dictionary<Collider2D, IInteractable> interactableCache = new Dictionary<Collider2D, IInteractable>();
        private ContactFilter2D interactableFilter;

        private IInteractable currentTarget;
        private Transform currentTargetTransform;
        private string lastShownControlScheme;

        private bool holding;
        private float holdElapsed;

        private void Awake()
        {
            interactableFilter = new ContactFilter2D();
            interactableFilter.SetLayerMask(GameLayers.InteractableMask);
            interactableFilter.useTriggers = true;
            if (inputReader == null) Debug.LogError("[PlayerInteractor] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerInteractor] hands 참조가 비어 있습니다.", this);
            if (heldItem == null) Debug.LogError("[PlayerInteractor] heldItem 참조가 비어 있습니다.", this);
            if (promptUI == null) Debug.LogError("[PlayerInteractor] promptUI 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerInteractor] settings 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.InteractPressed += HandleInteractPressed;
            inputReader.InteractReleased += HandleInteractReleased;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.InteractPressed -= HandleInteractPressed;
            inputReader.InteractReleased -= HandleInteractReleased;
        }

        private void Update()
        {
            // 맵 이동·지역 선택 창이 열린 동안은 안내도 숨긴다 (ARCHITECTURE.md 3.3).
            bool suppressed = inputReader != null && inputReader.IsLocked;
            (IInteractable nearest, Transform nearestTransform) = suppressed
                ? (null, null)
                : FindNearestInteractable();

            bool targetChanged = !ReferenceEquals(nearest, currentTarget);
            if (targetChanged) CancelHold();

            // target이 그대로여도, 보여 주고 있던 키 표시의 장치(키보드/패드)가 바뀌었으면 다시 그린다.
            string activeScheme = inputReader != null ? inputReader.ActiveControlScheme : null;
            bool schemeChangedWhileShown = !targetChanged && nearest != null && activeScheme != lastShownControlScheme;
            if (targetChanged || schemeChangedWhileShown)
            {
                currentTarget = nearest;
                currentTargetTransform = nearestTransform;
                lastShownControlScheme = activeScheme;
                RefreshPrompt();
            }

            TickHold();
        }

        private void TickHold()
        {
            if (!holding || currentTarget == null) return;
            if (suppressedByLock()) { CancelHold(); return; }

            holdElapsed += Time.deltaTime;
            float required = currentTarget.HoldSecondsRequired;
            if (holdElapsed >= required)
            {
                holding = false;
                holdElapsed = 0f;
                currentTarget.Interact(gameObject);
                RefreshPrompt(); // 대상의 안내 문구가 상호작용으로 바뀌었을 수 있다(문 열기/닫기 등).
                promptUI?.SetProgress(0f);
            }
            else
            {
                promptUI?.SetProgress(required > 0f ? holdElapsed / required : 1f);
            }
        }

        private bool suppressedByLock() => inputReader != null && inputReader.IsLocked;

        private void HandleInteractPressed()
        {
            if (currentTarget == null) return;
            if (currentTarget.HoldSecondsRequired <= 0f)
            {
                currentTarget.Interact(gameObject);
                RefreshPrompt(); // 대상의 안내 문구가 상호작용으로 바뀌었을 수 있다(문 열기/닫기 등).
                return;
            }
            holding = true;
            holdElapsed = 0f;
        }

        private void HandleInteractReleased() => CancelHold();

        private void CancelHold()
        {
            if (!holding) return;
            holding = false;
            holdElapsed = 0f;
            promptUI?.SetProgress(0f);
        }

        private void RefreshPrompt()
        {
            if (promptUI == null) return;
            if (currentTarget == null || currentTargetTransform == null)
            {
                promptUI.Hide();
                return;
            }
            string keyLabel = inputReader != null ? inputReader.GetInteractBindingDisplay() : "E";
            promptUI.Show(currentTargetTransform.position, $"[{keyLabel}] {currentTarget.InteractPrompt}");
        }

        private (IInteractable, Transform) FindNearestInteractable()
        {
            float radius = settings != null ? settings.InteractRadius : 0f;
            int count = Physics2D.OverlapCircle(transform.position, radius, interactableFilter, overlapResults);
            IInteractable nearest = null;
            Transform nearestTransform = null;
            float nearestSqrDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D overlapCollider = overlapResults[i];
                if (!interactableCache.TryGetValue(overlapCollider, out IInteractable candidate))
                {
                    candidate = overlapCollider.GetComponentInParent<IInteractable>();
                    interactableCache.Add(overlapCollider, candidate);
                }
                if (candidate == null) continue;
                if (!IsAllowed(candidate)) continue;
                float sqrDistance = ((Vector2)overlapResults[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
                nearestTransform = overlapResults[i].transform;
            }
            return (nearest, nearestTransform);
        }

        /// <summary>손 모드만으로 막히면, 사람 손이고 맞는 도구를 들고 있는지 한 번 더 물어본다
        /// (IToolGated, docs/design/GAME_DESIGN.md 7.1 "짐승 손 또는 맞는 도구").</summary>
        private bool IsAllowed(IInteractable candidate)
        {
            if (hands == null) return true;
            if (hands.AllowsHand(candidate.RequiredHand)) return true;
            if (hands.Mode != HandMode.Human || hands.IsTransitioning) return false;
            if (heldItem == null || !(candidate is IToolGated toolGated)) return false;
            int durability = heldItem.SelectedSlot?.Durability ?? -1;
            return toolGated.AllowsWithTool(heldItem.SelectedDefinition, durability);
        }
    }
}
