using Game.Items;

namespace Game.Maps
{
    /// <summary>
    /// 잠긴 문 판정 규칙. MonoBehaviour 없이 테스트할 수 있는 순수 함수만 둔다 (ARCHITECTURE.md 2장 원칙 4).
    /// 열쇠가 있으면 열쇠가 먼저다(조용히 열리고 내구도도 안 쓴다). 둘 다 사람 손이어야 한다(3.1).
    /// </summary>
    public static class LockedDoorRules
    {
        public static UnlockMethod Resolve(bool isHumanHand, bool hasKey, ItemDefinition selectedItem, int selectedDurability)
        {
            if (!isHumanHand) return UnlockMethod.None;
            if (hasKey) return UnlockMethod.Key;
            return IsUsableCrowbar(selectedItem, selectedDurability) ? UnlockMethod.Crowbar : UnlockMethod.None;
        }

        /// <summary>내구도가 다 닳은 쇠지렛대는 못 쓴다(부서지지는 않는다, 7장).</summary>
        public static bool IsUsableCrowbar(ItemDefinition item, int durability) =>
            item != null && item.IsTool && item.Tool.Kind == ToolKind.Crowbar && durability != 0;
    }
}
