using UnityEngine;

namespace Game.Harvesting
{
    /// <summary>채집물 하나가 주는 아이템 한 종류. 개수는 min~max 사이에서 고른다.</summary>
    [System.Serializable]
    public struct HarvestDrop
    {
        [SerializeField] private string itemId;
        [SerializeField] private int minCount;
        [SerializeField] private int maxCount;

        public string ItemId => itemId;
        public int RollCount() => Random.Range(minCount, maxCount + 1);
    }
}
