using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Maps;
using Game.Player;
using Game.World;
using UnityEngine;

namespace Game.Harvesting
{
    /// <summary>
    /// 정해진 땅 파기 지점 (docs/design/GAME_DESIGN.md 3.1, 4.3의 7단계 범위, ARCHITECTURE.md 3.4). 짐승 손으로만
    /// 눌러 담아 판다. 판 지점은 영구히 남는다(4.2). 종류는 두 가지를 데이터로 고른다.
    /// - 묻힌 상자: lootTable이 있으면 파낸 순간 그 구성이 가방에 들어온다(보상은 비공개 Data/Loot/ 에셋).
    /// - 울타리 밑 구멍: passageBlocker가 있으면 파낸 순간 꺼져 지나갈 수 있게 된다.
    /// 다 판 지점은 냄새도 나지 않는다(ScentSource를 끈다). State: 0=그대로, 1=파냄.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class DigSpot : MonoBehaviour, IInteractable, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private ExplorationSettings settings;
        [SerializeField] private Collider2D interactTrigger;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite undugSprite;
        [SerializeField] private Sprite dugSprite;
        [SerializeField] private ScentSource scentSource;
        [Tooltip("묻힌 상자: 파내면 가방에 들어오는 구성. 비워 두면 보상 없음.")]
        [SerializeField] private LootTableDefinition lootTable;
        [Tooltip("울타리 밑 구멍: 파내면 꺼지는 막는 콜라이더. 비워 두면 길이 열리지 않음.")]
        [SerializeField] private Collider2D passageBlocker;
        [SerializeField] private string interactPrompt = "파기";

        private bool dug;

        public string Id => persistentId != null ? persistentId.Id : "";
        public bool IsDug => dug;
        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Beast;
        public float HoldSecondsRequired => settings != null ? settings.DigHoldSeconds : 0f;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[DigSpot] persistentId 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[DigSpot] settings 참조가 비어 있습니다.", this);
            if (interactTrigger == null) Debug.LogError("[DigSpot] interactTrigger 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[DigSpot] visual 참조가 비어 있습니다.", this);
            RefreshVisual();
        }

        public void Interact(GameObject interactor)
        {
            if (dug) return;
            PlayerInventory inventory = interactor != null ? interactor.GetComponentInParent<PlayerInventory>() : null;
            if (inventory != null && lootTable != null)
            {
                foreach (ItemAmount amount in lootTable.Items)
                    {
                        int count = DifficultyRules.LootCount(amount.Count, GameServices.Difficulty);
                        if (count > 0) inventory.Collect(amount.ItemId, count);
                    }
            }
            else if (inventory == null && lootTable != null)
            {
                GameLog.Warning("DigSpot", "interactor에 PlayerInventory가 없습니다.", this);
            }
            SetDug(true);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(dug ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute) => SetDug(record.State == 1);

        private void SetDug(bool value)
        {
            dug = value;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (interactTrigger != null) interactTrigger.enabled = !dug;
            if (passageBlocker != null) passageBlocker.enabled = !dug;
            if (scentSource != null) scentSource.enabled = !dug;
            if (visual != null) visual.sprite = dug ? dugSprite : undugSprite;
        }
    }
}
