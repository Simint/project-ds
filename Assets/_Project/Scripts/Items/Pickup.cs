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
        // 가방이 꽉 찬 상태에서 주운 것(CollectWithState의 leftover)이 다시 Pickup으로 떨어지면, 플레이어
        // 트리거와 여전히 겹쳐 있어 바로 또 주워져 또 떨어지는 무한 반복이 된다. 생성 직후 잠깐은
        // 줍지 못하게 미뤄서 그 자리에서 되튀는 걸 끊는다(기술적 쿨다운 값이라 코드 상수로 둔다).
        private const float PickupDelaySeconds = 0.35f;

        private string itemId;
        private int count;
        private long spoilAtMinute;
        private int durability;
        private float pickupAllowedAtTime;

        /// <summary>갓 만들어진 스택이면 spoilAtMinute·durability를 0/-1로 둔다. 가방·보관함에서
        /// 꺼내다 흘린 스택이면 그 값을 그대로 넘겨서 흡수할 때 되돌려받게 한다(상태를 잃지 않는다).</summary>
        public void Initialize(string newItemId, int newCount, long newSpoilAtMinute = 0, int newDurability = -1)
        {
            itemId = newItemId;
            count = newCount;
            spoilAtMinute = newSpoilAtMinute;
            durability = newDurability;
            pickupAllowedAtTime = Time.time + PickupDelaySeconds;
        }

        // 겹친 순간(Enter)에 쿨다운에 걸리면, 그대로 안 움직이고 계속 겹쳐 있는 동안은 Enter가 다시
        // 안 불려서 영영 못 줍는다 — Stay로도 같이 받아서 쿨다운이 끝나는 대로 줍게 한다.
        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        private void TryCollect(Collider2D other)
        {
            if (Time.time < pickupAllowedAtTime) return;
            if (other.gameObject.layer != GameLayers.Player) return;
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            inventory.CollectWithState(itemId, count, spoilAtMinute, durability);
            Destroy(gameObject);
        }
    }
}
