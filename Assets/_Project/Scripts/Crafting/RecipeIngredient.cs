using UnityEngine;

namespace Game.Crafting
{
    /// <summary>레시피·건설에 필요한 재료 하나 (아이템 id + 수량). add-item/add-structure 스킬에서 쓰는
    /// ingredients/buildCost가 이 타입을 쓴다.</summary>
    [System.Serializable]
    public struct RecipeIngredient
    {
        [SerializeField] private string itemId;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public int Count => count;

        public RecipeIngredient(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }
}
