namespace Game.Crafting
{
    /// <summary>
    /// 은신처 시설 종류 (ARCHITECTURE.md 4.5). RecipeDefinition.Station(이 시설이 있어야 제작 가능)과
    /// Game.Building.StructureDefinition.Station(이 구조물이 그 시설 역할을 함)이 같은 값으로 잇는다.
    /// </summary>
    public enum StationKind
    {
        None,
        Workbench,
        Bed,
        Storage,
        FarmPlot,
        Stove,
        WaterPurifier,
        RainCollector,
        Greenhouse,
        Smokehouse,
        Generator,
        Decontamination
    }
}
