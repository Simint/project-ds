namespace Game.Crafting
{
    /// <summary>
    /// 제작·건설·수리가 재료를 세고 빼는 공통 창구 (ARCHITECTURE.md 4.4, docs/design/GAME_DESIGN.md 8).
    /// 가방 하나, 보관함 하나, 또는 여러 공급원을 묶은 것(MaterialSourceAggregator) 모두 이 인터페이스로
    /// 다룬다 — 작업대 UI·건설 모드·수리가 어떤 조합인지 몰라도 되게 한다.
    /// </summary>
    public interface IMaterialSource
    {
        int CountOf(string itemId);

        /// <summary>실제로 뺀 개수를 돌려준다 (모자라면 있는 만큼만).</summary>
        int Remove(string itemId, int count);
    }
}
