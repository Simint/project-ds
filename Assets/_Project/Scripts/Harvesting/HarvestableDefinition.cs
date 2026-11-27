using Game.Core;
using Game.Items;
using Game.Player;
using UnityEngine;

namespace Game.Harvesting
{
    /// <summary>
    /// 채집물 하나의 정의 (docs/design/GAME_DESIGN.md 7.1, add-harvestable 스킬). 덤불처럼 가벼운
    /// 것은 requiredHand=Any + holdSeconds=0(한 번), 나무·잔해처럼 도구가 필요한 것은
    /// requiredHand=Beast(맨손도 허용) + requiredToolKind(사람 손이면 이 도구가 있어야 함) +
    /// holdSeconds>0(길게 누르기)로 표현한다. 도구가 없으면 requiredToolKind=None으로 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Harvesting/Harvestable Definition")]
    public sealed class HarvestableDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private HandRequirement requiredHand = HandRequirement.Any;
        [SerializeField] private ToolKind requiredToolKind = ToolKind.None;
        [SerializeField] private int minToolTier;
        [SerializeField] private int toolDurabilityCostPerUse;
        [SerializeField] private float holdSeconds = 0f;
        [SerializeField] private HarvestDrop[] drops = System.Array.Empty<HarvestDrop>();
        [SerializeField] private int respawnDays = 0;
        [Tooltip("덤불·열매·풀 같은 식물이면 겨울에 채집할 수 없다 (docs/design/GAME_DESIGN.md 5.6).")]
        [SerializeField] private bool isPlant;

        public string Id => id;
        public string DisplayName => displayName;
        public HandRequirement RequiredHand => requiredHand;
        public ToolKind RequiredToolKind => requiredToolKind;
        public int MinToolTier => minToolTier;
        public int ToolDurabilityCostPerUse => toolDurabilityCostPerUse;
        public float HoldSeconds => holdSeconds;
        public HarvestDrop[] Drops => drops;

        /// <summary>0이면 리스폰하지 않는다.</summary>
        public int RespawnDays => respawnDays;
        public bool IsPlant => isPlant;
    }
}
