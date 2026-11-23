using UnityEngine;

namespace Game.Building
{
    /// <summary>침대 수치 (docs/design/GAME_DESIGN.md 8, MVP 5단계). 전부 임시값.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Sleep Settings")]
    public sealed class SleepSettings : ScriptableObject
    {
        [SerializeField] [Range(0f, 1f)] private float hungerThirstDrainMultiplier = 1f;
        [SerializeField] private float hpRegenMultiplier = 1f;
        [SerializeField] private int shortSleepHours;
        [SerializeField] private int mediumSleepHours;
        [SerializeField] private int longSleepHours;

        /// <summary>자는 동안 허기·갈증 감소 속도에 곱한다(1보다 작아야 덜 줄어든다).</summary>
        public float HungerThirstDrainMultiplier => hungerThirstDrainMultiplier;

        /// <summary>자는 동안 HP 자연 회복 속도에 곱한다(1보다 커야 더 빨리 회복한다).</summary>
        public float HpRegenMultiplier => hpRegenMultiplier;

        public int ShortSleepHours => shortSleepHours;
        public int MediumSleepHours => mediumSleepHours;
        public int LongSleepHours => longSleepHours;
    }
}
