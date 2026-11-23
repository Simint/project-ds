using Game.Core;
using Game.Crafting;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 건설물 하나의 정의 (ARCHITECTURE.md 4.5, add-structure 스킬 1절). footprint의 기준은 왼쪽 아래
    /// 셀이다. station이 None이 아니면 이 구조물이 그 시설 역할을 한다(작업대·침대·화로·보관함 등).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Building/Structure Definition")]
    public sealed class StructureDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private StructureKind kind = StructureKind.Prop;
        [SerializeField] private RecipeIngredient[] buildCost = System.Array.Empty<RecipeIngredient>();
        [SerializeField] private StationKind station = StationKind.None;
        [SerializeField] private int storageSlotCount;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Vector2Int Footprint => footprint;
        public StructureKind Kind => kind;
        public RecipeIngredient[] BuildCost => buildCost;

        /// <summary>None이 아니면 이 구조물이 그 시설이다(RecipeDefinition.Station과 같은 값).</summary>
        public StationKind Station => station;

        /// <summary>Station이 Storage일 때만 쓴다 — 보관함 칸 수.</summary>
        public int StorageSlotCount => storageSlotCount;
    }
}
