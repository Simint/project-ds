using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 은신처 양쪽의 잔해·쓰러진 나무 (docs/design/GAME_DESIGN.md 8, MVP 5단계). 길게 눌러 치우면
    /// 영구히 사라지고, 정해 둔 영역만큼 HubBuildArea가 열린다. 일반 맵의 Harvestable처럼
    /// PersistentId + MapStateRegistry로 저장된다(은신처도 MapLoader가 오가는 맵 중 하나라 똑같이
    /// 동작한다) — 건설한 구조물과 달리 리스폰하지 않고 에디터에 손으로 놓은 오브젝트라서 이 방식을 쓴다.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PersistentId))]
    public sealed class HubObstacle : MonoBehaviour, IInteractable, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private HubBuildArea buildArea;
        [SerializeField] private RectInt unlockArea;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D interactCollider;
        [SerializeField] private float holdSeconds;
        [SerializeField] private string interactPrompt = "치우기";

        private bool cleared;

        public string Id => persistentId != null ? persistentId.Id : "";
        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Beast;
        public float HoldSecondsRequired => holdSeconds;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[HubObstacle] persistentId 참조가 비어 있습니다.", this);
            if (buildArea == null) Debug.LogError("[HubObstacle] buildArea 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[HubObstacle] visual 참조가 비어 있습니다.", this);
            if (interactCollider == null) Debug.LogError("[HubObstacle] interactCollider 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (!cleared) SetCleared(true);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(cleared ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute) => SetCleared(record.State == 1);

        private void SetCleared(bool value)
        {
            cleared = value;
            if (cleared && buildArea != null) buildArea.UnlockArea(unlockArea);
            if (visual != null) visual.enabled = !cleared;
            if (interactCollider != null) interactCollider.enabled = !cleared;
        }
    }
}
