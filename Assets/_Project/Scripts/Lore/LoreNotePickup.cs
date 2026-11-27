using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Lore
{
    /// <summary>
    /// 맵에 놓은 쪽지 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위, ARCHITECTURE.md 3.4, 4.9). 집으면 바로 읽기 창이
    /// 뜨고 쪽지 기록에 들어간다. 주운 쪽지는 다시 생기지 않는다. State: 0=놓여 있음, 1=주움.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class LoreNotePickup : MonoBehaviour, IInteractable, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private LoreNoteDefinition note;
        [SerializeField] private Collider2D interactTrigger;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private string interactPrompt = "쪽지 읽기";

        private bool picked;

        public string Id => persistentId != null ? persistentId.Id : "";
        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[LoreNotePickup] persistentId 참조가 비어 있습니다.", this);
            if (note == null) Debug.LogError("[LoreNotePickup] note 참조가 비어 있습니다.", this);
            if (interactTrigger == null) Debug.LogError("[LoreNotePickup] interactTrigger 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[LoreNotePickup] visual 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (picked || note == null) return;
            GameServices.LoreCollection?.Add(note.Id);
            GameServices.LoreNotesUI?.ShowNote(note);
            SetPicked(true);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(picked ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute) => SetPicked(record.State == 1);

        private void SetPicked(bool value)
        {
            picked = value;
            if (interactTrigger != null) interactTrigger.enabled = !picked;
            if (visual != null) visual.enabled = !picked;
        }
    }
}
