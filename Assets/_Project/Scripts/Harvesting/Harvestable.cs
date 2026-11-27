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
        [SerializeField] private string winterPrompt = "겨울이라 채집할 수 없다";

        public string InteractPrompt => IsBlockedByWinter ? winterPrompt : definition != null ? definition.DisplayName : "";
        public HandRequirement RequiredHand => definition != null ? definition.RequiredHand : HandRequirement.Any;
        public float HoldSecondsRequired => IsBlockedByWinter ? 0f : definition != null ? definition.HoldSeconds : 0f;

        /// <summary>식물은 겨울에 채집할 수 없다(5.6 겨울).</summary>
        private bool IsBlockedByWinter => definition != null && definition.IsPlant && GameServices.Seasons != null && GameServices.Seasons.IsWinter;

        private void Awake()
        {
            if (definition == null) Debug.LogError("[Harvestable] definition 참조가 비어 있습니다.", this);
            if (persistentId == null) Debug.LogError("[Harvestable] persistentId 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[Harvestable] visual 참조가 비어 있습니다.", this);
            if (interactCollider == null) Debug.LogError("[Harvestable] interactCollider 참조가 비어 있습니다.", this);
        }

        public void Interact(GameObject interactor)
        {
            if (depleted || definition == null || IsBlockedByWinter) return;

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
            long respawnMinutes = DifficultyRules.RespawnMinutes(definition.RespawnDays, GameServices.Difficulty);
            long respawnAt = respawnMinutes > 0 ? nowMinute + respawnMinutes : 0;
            SetDepleted(true, respawnAt);
        }

        /// <summary>도구로 캤으면(짐승 손이 아니라 사람 손으로, 맞는 도구를 들고) 그 도구의 내구도를 깎는다.
        /// 짐승 손으로는 도구를 쓸 수 없으므로(맨손 채집으로 친다) 사람 손일 때만 깎는다.</summary>
        private void DamageHeldToolIfUsed(GameObject interactor)
        {
            if (definition.RequiredToolKind == ToolKind.None || definition.ToolDurabilityCostPerUse <= 0) return;
            PlayerHands hands = interactor != null ? interactor.GetComponentInParent<PlayerHands>() : null;
            if (hands != null && hands.Mode != HandMode.Human) return;

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

            // record.UntilMinute은 이미 계산이 끝난 "리스폰될 시각"이다. 아직 리스폰 전이면 그 값을 그대로
            // 쓴다 — SetDepleted에 nowMinute로 넘기면 거기서 또 RespawnDays를 더해 주기가 두 배로 늘어난다.
            bool respawnReady = definition != null && definition.RespawnDays > 0 && nowMinute >= record.UntilMinute;
            SetDepleted(!respawnReady, respawnReady ? 0 : record.UntilMinute);
        }

        /// <summary>depleted 여부와 그 상태의 콜라이더·스프라이트만 바꾼다. respawnAt은 호출하는 쪽이
        /// 이미 계산해서 넘긴다(인터랙트 시점엔 "지금 + 주기", 복원 시점엔 저장된 그 시각 그대로) — 여기서
        /// 다시 계산하지 않는다.</summary>
        private void SetDepleted(bool value, long respawnAt)
        {
            depleted = value;
            respawnAtMinute = value ? respawnAt : 0;

            if (visual != null) visual.enabled = !depleted;
            if (interactCollider != null) interactCollider.enabled = !depleted;
        }
    }
}
