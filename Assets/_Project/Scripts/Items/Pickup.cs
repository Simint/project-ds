using Game.Core;
using Game.Inventory;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 가방이 꽉 차서 땅에 떨어진 아이템 (docs/design/GAME_DESIGN.md 7.1). 플레이어가 닿으면
    /// 트리거로 자동으로 흡수된다. 플레이 중 생성되는 오브젝트라 PersistentId를 쓰지 않는다
    /// (ARCHITECTURE.md 2.3) — 맵을 나가면 사라진다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Pickup : MonoBehaviour
    {
        private string itemId;
        private int count;

        public void Initialize(string newItemId, int newCount)
        {
            itemId = newItemId;
            count = newCount;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != GameLayers.Player) return;
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            inventory.Collect(itemId, count);
            Destroy(gameObject);
        }
    }
}
