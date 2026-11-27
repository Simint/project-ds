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
        [SerializeField] private bool isConsumable;
        [SerializeField] private ConsumableData consumable = new ConsumableData();
        [SerializeField] private bool isTool;
        [SerializeField] private ToolData tool = new ToolData();
        [SerializeField] private bool isWeapon;
        [SerializeField] private WeaponData weapon = new WeaponData();

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStackSize => maxStackSize;
        public float Weight => weight;
        public ItemCategory Category => category;
        public HandRequirement RequiredHand => requiredHand;

        /// <summary>단축칸 사용 키로 쓸 수 있는 아이템인지 (음식·물·의약품, docs/design/GAME_DESIGN.md 3.1).</summary>
        public bool IsConsumable => isConsumable;
        public ConsumableData Consumable => consumable;

        /// <summary>도끼·곡괭이처럼 내구도가 있고 채집물 조건에 쓰이는 도구인지.</summary>
        public bool IsTool => isTool;
        public ToolData Tool => tool;

        /// <summary>사람 손으로 근접 공격에 쓸 수 있는 무기인지 (docs/design/GAME_DESIGN.md 6.1).</summary>
        public bool IsWeapon => isWeapon;
        public WeaponData Weapon => weapon;

        /// <summary>내구도나 연료가 있는 아이템은 겹쳐 쌓지 않는다 (ARCHITECTURE.md 4.4).</summary>
        public int EffectiveMaxStackSize => isTool ? 1 : maxStackSize;
    }
}
