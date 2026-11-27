namespace Game.Items
{
    /// <summary>도구 종류. 채집물의 requiredToolKind와 맞춘다 (add-harvestable/add-item 스킬).</summary>
    public enum ToolKind
    {
        None,
        Axe,
        Pickaxe,
        /// <summary>잠긴 문을 열쇠 없이 연다 (docs/design/GAME_DESIGN.md 4.3 7단계 범위).</summary>
        Crowbar
    }
}
