namespace Game.Building
{
    /// <summary>건설물 종류. 레이어·충돌·설치 조건을 이 값 하나가 정한다 (add-structure 스킬 0절).</summary>
    public enum StructureKind
    {
        Solid,
        Platform,
        BackWall,
        Prop,
        Door
    }
}
