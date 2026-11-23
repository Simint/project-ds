using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Crafting
{
    /// <summary>
    /// 제작·수리 레시피 하나 (ARCHITECTURE.md 4.4, add-item 스킬 3절). isRepair면 resultItemId는 새로
    /// 만드는 아이템이 아니라 "이 id의 아이템을 골라 내구도를 채운다"는 뜻이고 resultCount는 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Crafting/Recipe Definition")]
    public sealed class RecipeDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private RecipeIngredient[] ingredients = System.Array.Empty<RecipeIngredient>();
        [SerializeField] private string resultItemId = "";
        [SerializeField] private int resultCount = 1;
        [SerializeField] private StationKind station = StationKind.None;
        [SerializeField] private HandRequirement requiredHand = HandRequirement.Human;
        [SerializeField] private bool isRepair;

        public string Id => id;
        public string DisplayName => displayName;
        public RecipeIngredient[] Ingredients => ingredients;
        public string ResultItemId => resultItemId;
        public int ResultCount => resultCount;

        /// <summary>None이면 손 제작(시설 필요 없음).</summary>
        public StationKind Station => station;
        public HandRequirement RequiredHand => requiredHand;

        /// <summary>true면 결과물을 새로 만드는 대신, ResultItemId와 같은 종류의 도구 한 개를 골라
        /// 내구도를 최대로 채운다 (ARCHITECTURE.md 4.4 "수리").</summary>
        public bool IsRepair => isRepair;
    }
}
