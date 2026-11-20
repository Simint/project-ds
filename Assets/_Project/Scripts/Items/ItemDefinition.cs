using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 아이템 하나의 정의 (ARCHITECTURE.md 4.4). 도구·무기·음식 같은 종류별 데이터(ToolData 등)는
    /// 그 종류가 실제로 생기는 단계에서 추가한다 — 지금은 재료 아이템만 있어 필요 없다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStackSize = 20;
        [SerializeField] private float weight = 0.1f;
        [SerializeField] private ItemCategory category = ItemCategory.Material;
        [SerializeField] private HandRequirement requiredHand = HandRequirement.Any;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStackSize => maxStackSize;
        public float Weight => weight;
        public ItemCategory Category => category;
        public HandRequirement RequiredHand => requiredHand;
    }
}
