using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using Game.World;
using UnityEngine;

namespace Game.Harvesting
{
    /// <summary>
    /// 나무·덤불·잔해 더미 같은 채집물 (ARCHITECTURE.md 3.4, 4.2, add-harvestable 스킬). 완료되면
    /// 정의된 아이템을 바로 플레이어 가방에 주고, 리스폰 일수가 지날 때까지 숨는다. 도구를 쓴 채집이면
    /// 그 도구의 내구도를 깎는다(0이 되면 못 쓸 뿐 사라지지 않는다, docs/design/GAME_DESIGN.md 7).
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PersistentId))]
    public sealed class Harvestable : MonoBehaviour, IInteractable, IPersistentObject, IToolGated
    {
        [SerializeField] private HarvestableDefinition definition;
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D interactCollider;

        private bool depleted;
        private long respawnAtMinute;

        public string Id => persistentId != null ? persistentId.Id : "";
        public string InteractPrompt => definition != null ? definition.DisplayName : "";
        public HandRequirement RequiredHand => definition != null ? definition.RequiredHand : HandRequirement.Any;
        public float HoldSecondsRequired => definition != null ? definition.HoldSeconds : 0f;

        private void Awake()
        {
            if (definition == null) Debug.LogError("[Harvestable] definition 참조가 비어 있습니다.", this);
            if (persistentId == null) Debug.LogError("[Harvestable] persistentId 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[Harvestable] visual 참조가 비어 있습니다.", this);
            if (interactCollider == null) Debug.LogError("[Harvestable] interactCollider 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (depleted || definition == null) return;

            PlayerInventory inventory = interactor != null ? interactor.GetComponentInParent<PlayerInventory>() : null;
            if (inventory == null)
            {
                GameLog.Warning("Harvestable", "interactor에 PlayerInventory가 없습니다.", this);
                return;
            }

            foreach (HarvestDrop drop in definition.Drops)
            {
                int count = drop.RollCount();
                if (count > 0) inventory.Collect(drop.ItemId, count);
            }

            DamageHeldToolIfUsed(interactor);

            long nowMinute = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            SetDepleted(true, nowMinute);
        }

        /// <summary>도구로 캤으면(짐승 손이 아니라 사람 손으로, 맞는 도구를 들고) 그 도구의 내구도를 깎는다.</summary>
        private void DamageHeldToolIfUsed(GameObject interactor)
        {
            if (definition.RequiredToolKind == ToolKind.None || definition.ToolDurabilityCostPerUse <= 0) return;
            PlayerHeldItem heldItem = interactor != null ? interactor.GetComponentInParent<PlayerHeldItem>() : null;
            if (heldItem == null) return;

            InventorySlot selectedSlot = heldItem.SelectedSlot;
            ItemDefinition selected = heldItem.SelectedDefinition;
            if (selectedSlot == null || !AllowsWithTool(selected, selectedSlot.Durability)) return;

            heldItem.Inventory.Model.DamageTool(heldItem.SelectedSlotIndex, definition.ToolDurabilityCostPerUse);
        }

        public bool AllowsWithTool(ItemDefinition selectedItem, int durability)
        {
            if (definition == null || definition.RequiredToolKind == ToolKind.None) return false;
            if (selectedItem == null || !selectedItem.IsTool) return false;
            if (durability == 0) return false; // 내구도가 다 닳으면 못 쓴다(부서지지는 않는다).
            return selectedItem.Tool.Kind == definition.RequiredToolKind && selectedItem.Tool.Tier >= definition.MinToolTier;
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(depleted ? 1 : 0, respawnAtMinute, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            if (record.State == 0)
            {
                SetDepleted(false, 0);
                return;
            }

            bool respawnReady = definition != null && definition.RespawnDays > 0 && nowMinute >= record.UntilMinute;
            SetDepleted(!respawnReady, respawnReady ? 0 : record.UntilMinute);
        }

        private void SetDepleted(bool value, long nowMinute)
        {
            depleted = value;
            respawnAtMinute = value && definition != null && definition.RespawnDays > 0
                ? nowMinute + definition.RespawnDays * WorldClock.MinutesPerDay
                : 0;

            if (visual != null) visual.enabled = !depleted;
            if (interactCollider != null) interactCollider.enabled = !depleted;
        }
    }
}
