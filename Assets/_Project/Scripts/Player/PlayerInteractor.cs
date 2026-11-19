using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 상호작용 버튼을 누르면 주변 Interactable 레이어에서 가장 가까운 IInteractable을 찾아 실행한다
    /// (ARCHITECTURE.md 4.2). 결과 버퍼는 미리 할당해 둔다.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private float interactRadius = 1f;

        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private ContactFilter2D interactableFilter;

        private void Awake()
        {
            interactableFilter = new ContactFilter2D();
            interactableFilter.SetLayerMask(GameLayers.InteractableMask);
            interactableFilter.useTriggers = true;
            if (inputReader == null) Debug.LogError("[PlayerInteractor] inputReader 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.InteractPressed += HandleInteractPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.InteractPressed -= HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            IInteractable nearest = FindNearestInteractable();
            nearest?.Interact(gameObject);
        }

        private IInteractable FindNearestInteractable()
        {
            int count = Physics2D.OverlapCircle(transform.position, interactRadius, interactableFilter, overlapResults);
            IInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = overlapResults[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;
                float sqrDistance = ((Vector2)overlapResults[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
            return nearest;
        }
    }
}
