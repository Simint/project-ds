using Game.Core;
using Game.Inventory;
using Game.Player;
using Game.World;
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
        [Tooltip("뒤지면 가방에 들어오는 구성. 보상은 밸런스라 비공개 Data/Loot/ 에셋을 참조만 한다.")]
        [SerializeField] private LootTableDefinition lootTable;
        [SerializeField] private string interactPrompt = "뒤지기";
        [Tooltip("후각 탐지 대상(선택). 털리면 꺼서 더 이상 냄새가 나지 않게 한다.")]
        [SerializeField] private ScentSource scentSource;

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

            if (lootTable != null)
            {
                foreach (ItemAmount amount in lootTable.Items)
                    {
                        int count = DifficultyRules.LootCount(amount.Count, GameServices.Difficulty);
                        if (count > 0) inventory.Collect(amount.ItemId, count);
                    }
            }

            SetLooted(true);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(looted ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            SetLooted(record.State == 1);
        }

        private void SetLooted(bool value)
        {
            looted = value;
            if (scentSource != null) scentSource.enabled = !looted;
        }
    }
}
