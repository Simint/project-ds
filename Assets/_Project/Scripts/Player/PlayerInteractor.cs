using System.Collections.Generic;
using Game.Core;
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
        [SerializeField] private InteractPromptUI promptUI;
        [SerializeField] private float interactRadius = 1f;

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
            if (promptUI == null) Debug.LogError("[PlayerInteractor] promptUI 참조가 비어 있습니다.", this);
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
            int count = Physics2D.OverlapCircle(transform.position, interactRadius, interactableFilter, overlapResults);
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
                if (hands != null && !hands.AllowsHand(candidate.RequiredHand)) continue;
                float sqrDistance = ((Vector2)overlapResults[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
                nearestTransform = overlapResults[i].transform;
            }
            return (nearest, nearestTransform);
        }
    }
}
