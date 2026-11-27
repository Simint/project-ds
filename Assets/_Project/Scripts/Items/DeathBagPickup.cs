using Game.Core;
using Game.Inventory;
using Game.Player;
using Game.Save;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 사망 주머니 (docs/design/GAME_DESIGN.md 5.8, ARCHITECTURE.md 4.8). MapLoader가 사망한 맵에
    /// 도착했을 때 DeathBagRegistry 내용을 기준으로 이 자리에 만든다. 열면(E) 전부 가방으로 돌아가고
    /// 기록이 지워진다 — Container와 달리 PersistentId를 쓰지 않는다(맵마다 하나뿐이고 레지스트리가
    /// 이미 "있는지"를 들고 있어서, 떠나면 사라지는 일반 Pickup과 같은 생성 방식이면 충분하다).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DeathBagPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactPrompt = "소지품 주머니 줍기";

        public string InteractPrompt => interactPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        public void Interact(GameObject interactor)
        {
            PlayerInventory inventory = interactor != null ? interactor.GetComponentInParent<PlayerInventory>() : null;
            if (inventory == null)
            {
                GameLog.Warning("DeathBagPickup", "interactor에 PlayerInventory가 없습니다.", this);
                return;
            }

            DeathBagRegistry registry = GameServices.DeathBagRegistry;
            if (registry == null) return;

            foreach (InventorySlotData slot in registry.Current.items)
                inventory.CollectWithState(slot.itemId, slot.count, slot.spoilAtMinute, slot.durability);

            registry.Clear();
            Destroy(gameObject);
        }
    }
}
