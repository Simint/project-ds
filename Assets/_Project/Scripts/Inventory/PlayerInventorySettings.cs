using UnityEngine;

namespace Game.Inventory
{
    /// <summary>플레이어 가방의 칸 수·무게 한도 (ARCHITECTURE.md 4.4). 밸런스 값이라 코드에 두지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Inventory/Player Inventory Settings")]
    public sealed class PlayerInventorySettings : ScriptableObject
    {
        [SerializeField] private int slotCount = 0;
        [SerializeField] private float weightLimit = 0f;

        public int SlotCount => slotCount;
        public float WeightLimit => weightLimit;
    }
}
