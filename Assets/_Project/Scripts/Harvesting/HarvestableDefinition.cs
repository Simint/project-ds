using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Harvesting
{
    /// <summary>
    /// 채집물 하나의 정의 (docs/design/GAME_DESIGN.md 7.1, add-harvestable 스킬). 덤불처럼 가벼운
    /// 것은 requiredHand=Any + holdSeconds=0(한 번), 나무·잔해처럼 도구가 필요한 것은
    /// requiredHand=Beast(도구가 생기기 전까지) + holdSeconds>0(길게 누르기)로 표현한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Harvesting/Harvestable Definition")]
    public sealed class HarvestableDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private HandRequirement requiredHand = HandRequirement.Any;
        [SerializeField] private float holdSeconds = 0f;
        [SerializeField] private HarvestDrop[] drops = System.Array.Empty<HarvestDrop>();
        [SerializeField] private int respawnDays = 0;

        public string Id => id;
        public string DisplayName => displayName;
        public HandRequirement RequiredHand => requiredHand;
        public float HoldSeconds => holdSeconds;
        public HarvestDrop[] Drops => drops;

        /// <summary>0이면 리스폰하지 않는다.</summary>
        public int RespawnDays => respawnDays;
    }
}
