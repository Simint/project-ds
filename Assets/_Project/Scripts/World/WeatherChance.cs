using UnityEngine;

namespace Game.World
{
    /// <summary>날씨 하나가 계절마다 뽑힐 가중치 (WeatherSettings 목록 한 줄). 0이면 그 계절에는 나오지 않는다.</summary>
    [System.Serializable]
    public sealed class WeatherChance
    {
        [SerializeField] private WeatherDefinition weather;
        [SerializeField] [Min(0f)] private float springWeight = 0f;
        [SerializeField] [Min(0f)] private float summerWeight = 0f;
        [SerializeField] [Min(0f)] private float autumnWeight = 0f;
        [SerializeField] [Min(0f)] private float winterWeight = 0f;

        public WeatherChance() { }

        public WeatherChance(WeatherDefinition weather, float spring, float summer, float autumn, float winter)
        {
            this.weather = weather;
            springWeight = spring;
            summerWeight = summer;
            autumnWeight = autumn;
            winterWeight = winter;
        }

        public WeatherDefinition Weather => weather;

        public float WeightFor(Season season) => season switch
        {
            Season.Summer => summerWeight,
            Season.Autumn => autumnWeight,
            Season.Winter => winterWeight,
            _ => springWeight
        };
    }
}
