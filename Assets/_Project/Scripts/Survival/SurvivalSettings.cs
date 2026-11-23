using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 스탯 하나로 설명되지 않는 생존 규칙의 수치 (docs/design/GAME_DESIGN.md 5.2, 5.4, MVP 4단계).
    /// 전부 임시값이고 플레이하며 조절한다 (.claude/rules/csharp.md "수치와 이름").
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Survival/Survival Settings")]
    public sealed class SurvivalSettings : ScriptableObject
    {
        [Header("HP 감소·회복 (허기·갈증·체온)")]
        [SerializeField] private float hpDrainPerHourWhenStarving = -4f;
        [SerializeField] private float hpDrainPerHourWhenCold = -4f;
        [SerializeField] private float hpRegenPerHourWhenHealthy = 2f;
        [SerializeField] private float hpRegenMinHunger = 50f;
        [SerializeField] private float hpRegenMinThirst = 50f;
        [SerializeField] [Range(0f, 1f)] private float reviveHpFraction = 0.5f;

        [Header("스태미나 (달리기)")]
        [SerializeField] private float staminaDrainPerSecondRun = 8f;
        [SerializeField] private float staminaDrainPerSecondBeastRun = 12f;
        [SerializeField] private float staminaRegenPerSecond = 10f;
        [SerializeField] [Range(0f, 1f)] private float sicknessStaminaRegenMultiplier = 0.5f;

        [Header("손 모드")]
        [SerializeField] private float beastHandHungerMultiplier = 1.5f;

        [Header("체온 (실외 밤에 하락, 그 외에는 서서히 회복)")]
        [SerializeField] private float nightOutdoorTemperatureDropPerHour = -8f;
        [SerializeField] private float temperatureRecoveryPerHourOutdoorDay = 4f;
        [SerializeField] private float temperatureRecoveryPerHourIndoor = 10f;
        [SerializeField] [Range(0, 23)] private int nightStartHour = 20;
        [SerializeField] [Range(0, 23)] private int nightEndHour = 6;

        [Header("낙하 피해·골절")]
        [SerializeField] private float fallDamageThresholdTiles = 4f;
        [SerializeField] private float fallDamagePerExcessTile = 5f;
        [SerializeField] private float fractureFallThresholdTiles = 8f;

        [Header("상한 음식·더러운 물")]
        [SerializeField] [Range(0f, 1f)] private float dirtyWaterSicknessChance = 0.3f;
        [SerializeField] private float dirtyWaterThirstRestore = 30f;

        public float HpDrainPerHourWhenStarving => hpDrainPerHourWhenStarving;
        public float HpDrainPerHourWhenCold => hpDrainPerHourWhenCold;
        public float HpRegenPerHourWhenHealthy => hpRegenPerHourWhenHealthy;
        public float HpRegenMinHunger => hpRegenMinHunger;
        public float HpRegenMinThirst => hpRegenMinThirst;
        public float ReviveHpFraction => reviveHpFraction;

        public float StaminaDrainPerSecondRun => staminaDrainPerSecondRun;
        public float StaminaDrainPerSecondBeastRun => staminaDrainPerSecondBeastRun;
        public float StaminaRegenPerSecond => staminaRegenPerSecond;
        public float SicknessStaminaRegenMultiplier => sicknessStaminaRegenMultiplier;

        public float BeastHandHungerMultiplier => beastHandHungerMultiplier;

        public float NightOutdoorTemperatureDropPerHour => nightOutdoorTemperatureDropPerHour;
        public float TemperatureRecoveryPerHourOutdoorDay => temperatureRecoveryPerHourOutdoorDay;
        public float TemperatureRecoveryPerHourIndoor => temperatureRecoveryPerHourIndoor;
        public int NightStartHour => nightStartHour;
        public int NightEndHour => nightEndHour;

        public float FallDamageThresholdTiles => fallDamageThresholdTiles;
        public float FallDamagePerExcessTile => fallDamagePerExcessTile;
        public float FractureFallThresholdTiles => fractureFallThresholdTiles;

        public float DirtyWaterSicknessChance => dirtyWaterSicknessChance;
        public float DirtyWaterThirstRestore => dirtyWaterThirstRestore;

        /// <summary>현재 게임 내 시각(분)이 밤 시간대인지. nightStartHour > nightEndHour면 자정을 건너간다.</summary>
        public bool IsNightHour(int hour)
        {
            return nightStartHour > nightEndHour
                ? (hour >= nightStartHour || hour < nightEndHour)
                : (hour >= nightStartHour && hour < nightEndHour);
        }

        /// <summary>
        /// 체온의 environment 모디파이어를 상황에 맞게 고른다 (ARCHITECTURE.md 4.3, PlayerSurvival이
        /// HourPassed·MapChanged 때마다 다시 부른다). 실외 밤에는 내려가고, 그 외(실내·은신처, 실외 낮)에는
        /// 서서히 올라간다 — 실제 값 변화는 SurvivalStat.Add의 clamp가 최대치에서 멈춘다.
        /// </summary>
        public StatModifier ComputeTemperatureModifier(bool isSafeFromWeather, int hour, float extraRecoveryPerHour = 0f)
        {
            float baseRate = !isSafeFromWeather && IsNightHour(hour)
                ? nightOutdoorTemperatureDropPerHour
                : (isSafeFromWeather ? temperatureRecoveryPerHourIndoor : temperatureRecoveryPerHourOutdoorDay);
            return StatModifier.Additive(baseRate + extraRecoveryPerHour);
        }
    }
}
