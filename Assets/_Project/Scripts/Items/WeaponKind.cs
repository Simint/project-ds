namespace Game.Items
{
    /// <summary>근접 무기 종류 (docs/design/GAME_DESIGN.md 6.1). Blade는 총·발톱과 함께 출혈 연출의
    /// 대상이 될 수 있지만, 지금(MVP 6단계)은 둔기와 날붙이 모두 피해·넉백만 다르다.</summary>
    public enum WeaponKind
    {
        Blunt,
        Blade
    }
}
