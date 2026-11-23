using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 도구 아이템의 데이터 (ARCHITECTURE.md 4.4). maxDurability가 있는 아이템은 최대 스택이 1이다
    /// (ItemDefinition에서 보장). 0이 되면 못 쓰지만 사라지지 않고, 작업대에서 재료로 수리한다
    /// (docs/design/GAME_DESIGN.md 7).
    /// </summary>
    [System.Serializable]
    public sealed class ToolData
    {
        [SerializeField] private ToolKind kind = ToolKind.None;
        [SerializeField] private int tier;
        [SerializeField] private int maxDurability;

        public ToolKind Kind => kind;
        public int Tier => tier;
        public int MaxDurability => maxDurability;
    }
}
