using Game.Core;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 날씨 하나 (ARCHITECTURE.md 4.1 WeatherDefinition, docs/design/GAME_DESIGN.md 5.6). 효과는 실외에서만 적용된다.
    /// 배율 기본값은 1(영향 없음), 나머지는 0이다. 실제 값은 비공개 Data/World/Weather/ 에셋에만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Weather Definition")]
    public sealed class WeatherDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private WeatherKind kind = WeatherKind.Clear;

        [Header("실외 효과")]
        [Tooltip("시간당 체온 변화에 더하는 값(음수 = 식는다).")]
        [SerializeField] private float temperaturePerHour = 0f;
        [SerializeField] [Min(0f)] private float playerSightMultiplier = 1f;
        [SerializeField] [Min(0f)] private float enemySightMultiplier = 1f;
        [Tooltip("적이 듣는 소음 반경 배율(폭우는 빗소리로 소음을 묻는다).")]
        [SerializeField] [Min(0f)] private float noiseRadiusMultiplier = 1f;
        [Tooltip("빗물 통에 물이 차는지.")]
        [SerializeField] private bool fillsRainBarrels;

        [Header("지속 시간 (게임 분)")]
        [SerializeField] [Min(0)] private int minDurationMinutes = 0;
        [SerializeField] [Min(0)] private int maxDurationMinutes = 0;

        [Header("표시")]
        [Tooltip("안개 등 화면에 덮는 색(알파 = 짙기). 투명이면 덮지 않는다.")]
        [SerializeField] private Color overlayColor = Color.clear;

        public string Id => id;
        public string DisplayName => displayName;
        public WeatherKind Kind => kind;
        public float TemperaturePerHour => temperaturePerHour;
        public float PlayerSightMultiplier => playerSightMultiplier;
        public float EnemySightMultiplier => enemySightMultiplier;
        public float NoiseRadiusMultiplier => noiseRadiusMultiplier;
        public bool FillsRainBarrels => fillsRainBarrels;
        public int MinDurationMinutes => minDurationMinutes;
        public int MaxDurationMinutes => maxDurationMinutes;
        public Color OverlayColor => overlayColor;
    }
}
