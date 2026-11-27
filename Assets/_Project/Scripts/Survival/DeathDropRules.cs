using Game.Items;
using Game.World;

namespace Game.Survival
{
    /// <summary>
    /// 죽을 때 어떤 물건을 사망 주머니로 떨어뜨리는지 (docs/design/GAME_DESIGN.md 5.8 난이도 표). 쉬움은 재료류만,
    /// 보통·어려움은 가방 전부. 어려움의 "장착 장비까지"는 장비 칸이 생기면 그쪽에서 이 범위를 보고 함께 떨어뜨린다.
    /// </summary>
    public static class DeathDropRules
    {
        public static bool ShouldDrop(ItemCategory category, DeathDropScope scope) =>
            scope != DeathDropScope.MaterialsOnly || category == ItemCategory.Material;

        public static bool DropsEquipment(DeathDropScope scope) => scope == DeathDropScope.BagAndEquipment;
    }
}
