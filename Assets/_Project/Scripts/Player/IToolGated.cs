using Game.Items;

namespace Game.Player
{
    /// <summary>
    /// RequiredHand만으로 손 모드를 못 가를 때 쓴다 (ARCHITECTURE.md 4.2, docs/design/GAME_DESIGN.md 7.1:
    /// "짐승 손(맨손) 또는 맞는 도구"). PlayerInteractor는 RequiredHand 검사가 막히면, 대상이 이 인터페이스를
    /// 구현하고 플레이어가 사람 손일 때만 AllowsWithTool을 한 번 더 물어본다. durability는 선택한 슬롯의
    /// 내구도다(-1 = 내구도 없음) — 0이면 도구가 닳아서 못 쓴다(docs/design/GAME_DESIGN.md 7).
    /// </summary>
    public interface IToolGated
    {
        bool AllowsWithTool(ItemDefinition selectedItem, int durability);
    }
}
