using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 날씨 목록과 계절별 가중치 (ARCHITECTURE.md 4.1 WeatherSystem). 맑음도 목록의 한 줄이다. 눈은 겨울 가중치만 두어
    /// 겨울에만 나오게 한다(5.6). 실제 값은 비공개 Data/World/ 에셋에만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Weather Settings")]
    public sealed class WeatherSettings : ScriptableObject
    {
        [Tooltip("목록이 비었거나 뽑을 수 없을 때 쓰는 날씨(맑음).")]
        [SerializeField] private WeatherDefinition fallback;
        [SerializeField] private List<WeatherChance> chances = new List<WeatherChance>();

        public WeatherDefinition Fallback => fallback;
        public IReadOnlyList<WeatherChance> Chances => chances;

        public WeatherDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (fallback != null && fallback.Id == id) return fallback;
            for (int i = 0; i < chances.Count; i++)
                if (chances[i]?.Weather != null && chances[i].Weather.Id == id) return chances[i].Weather;
            return null;
        }
    }
}
