namespace Game.World
{
    /// <summary>죽을 때 사망 주머니로 떨어뜨리는 범위 (docs/design/GAME_DESIGN.md 5.8 난이도 표).</summary>
    public enum DeathDropScope
    {
        /// <summary>쉬움: 가방 안 물건 중 재료류만.</summary>
        MaterialsOnly,
        /// <summary>보통: 가방 안 물건 전부, 장착 장비는 유지.</summary>
        Bag,
        /// <summary>어려움: 장착 장비까지 전부. 장비 칸이 생기기 전까지는 Bag과 같다.</summary>
        BagAndEquipment
    }
}
