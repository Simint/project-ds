using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 먹거나 쓸 수 있는 아이템의 효과 (ARCHITECTURE.md 4.4, docs/design/GAME_DESIGN.md 5.5, 5.4).
    /// spoilMinutes가 0보다 크면 부패하는 아이템이고, spoiledItemId가 그 뒤 표시·사용에 쓰일 아이템이다
    /// (부패는 lazy — InventorySlot.SpoilAtMinute과 비교해서 그때그때 판단, 저장된 itemId는 바꾸지 않는다).
    /// curesStatusEffectId는 붕대·부목·약처럼 상태 이상을 치료하는 아이템에 쓴다.
    /// </summary>
    [System.Serializable]
    public sealed class ConsumableData
    {
        [SerializeField] private StatRestoreAmount[] statRestores = System.Array.Empty<StatRestoreAmount>();
        [SerializeField] private int spoilMinutes = 0;
        [SerializeField] private string spoiledItemId = "";
        [SerializeField] [Range(0f, 1f)] private float sicknessChance = 0f;
        [SerializeField] private string curesStatusEffectId = "";

        public StatRestoreAmount[] StatRestores => statRestores;

        /// <summary>0이면 상하지 않는다.</summary>
        public int SpoilMinutes => spoilMinutes;
        public string SpoiledItemId => spoiledItemId;
        public float SicknessChance => sicknessChance;
        public string CuresStatusEffectId => curesStatusEffectId;
    }
}
