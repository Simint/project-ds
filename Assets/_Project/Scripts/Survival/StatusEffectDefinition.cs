using Game.Core;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 상태 이상 하나의 정의 (ARCHITECTURE.md 4.3, docs/design/GAME_DESIGN.md 5.4). durationMinutes가
    /// 0이면 치료 아이템(ItemDefinition.Consumable.CuresStatusEffectId)으로만 풀린다. moveSpeedMultiplier·
    /// jumpMultiplier는 PlayerController.SetSpeedMultiplier/SetJumpMultiplier에 그대로 전달된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Survival/Status Effect Definition")]
    public sealed class StatusEffectDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private long durationMinutes = 0;
        [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private float jumpMultiplier = 1f;
        [SerializeField] private StatRateModifierEntry[] statRateModifiers = System.Array.Empty<StatRateModifierEntry>();
        [SerializeField] private Color uiColor = Color.white;

        public string Id => id;
        public string DisplayName => displayName;

        /// <summary>0이면 치료할 때까지 지속. 0보다 크면 그만큼 지난 뒤 저절로 풀린다.</summary>
        public long DurationMinutes => durationMinutes;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float JumpMultiplier => jumpMultiplier;
        public StatRateModifierEntry[] StatRateModifiers => statRateModifiers;
        public Color UiColor => uiColor;
    }
}
