using UnityEngine;

namespace Game.Building
{
    /// <summary>화로 수치 (docs/design/GAME_DESIGN.md 8, MVP 5단계). 전부 임시값.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Stove Settings")]
    public sealed class StoveSettings : ScriptableObject
    {
        [SerializeField] private string fuelItemId;
        [SerializeField] private int fuelMinutesPerWoodLog;
        [SerializeField] private float heatZoneRadiusTiles;
        [SerializeField] private float heatZoneExtraRecoveryPerHour;

        public string FuelItemId => fuelItemId;
        public int FuelMinutesPerWoodLog => fuelMinutesPerWoodLog;
        public float HeatZoneRadiusTiles => heatZoneRadiusTiles;
        public float HeatZoneExtraRecoveryPerHour => heatZoneExtraRecoveryPerHour;
    }
}
