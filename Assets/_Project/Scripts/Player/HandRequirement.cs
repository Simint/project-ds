namespace Game.Player
{
    /// <summary>
    /// 행동(상호작용·아이템·레시피)이 요구하는 손 모드. 허용 검사는 모두 이 값 하나로 통일한다
    /// (ARCHITECTURE.md 4.2, docs/design/GAME_DESIGN.md 3.3).
    /// </summary>
    public enum HandRequirement
    {
        Any,
        Human,
        Beast
    }
}
