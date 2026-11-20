using Game.Core;
using Game.Inventory;
using Game.Player;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 캐비닛 같은 컨테이너 (docs/design/GAME_DESIGN.md 7.1). E 한 번에 내용물이 통째로 가방에
    /// 들어가고, 털어 간 상태가 저장되어 다시 차지 않는다 (ARCHITECTURE.md 3.4).
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PersistentId))]
    public sealed class Container : MonoBehaviour, IInteractable, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private ItemAmount[] contents = System.Array.Empty<ItemAmount>();
        [SerializeField] private string interactPrompt = "뒤지기";

        private bool looted;

        public string Id => persistentId != null ? persistentId.Id : "";
        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[Container] persistentId 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (looted) return;

            PlayerInventory inventory = interactor != null ? interactor.GetComponentInParent<PlayerInventory>() : null;
            if (inventory == null)
            {
                GameLog.Warning("Container", "interactor에 PlayerInventory가 없습니다.", this);
                return;
            }

            foreach (ItemAmount amount in contents)
                if (amount.Count > 0) inventory.Collect(amount.ItemId, amount.Count);

            looted = true;
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(looted ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            looted = record.State == 1;
        }
    }
}
