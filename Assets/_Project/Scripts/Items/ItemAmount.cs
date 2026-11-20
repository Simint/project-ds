using UnityEngine;

namespace Game.Items
{
    /// <summary>아이템 id + 정해진 개수. 캐비닛처럼 손으로 놓은 컨테이너의 내용물을 적을 때 쓴다.</summary>
    [System.Serializable]
    public struct ItemAmount
    {
        [SerializeField] private string itemId;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public int Count => count;
    }
}
