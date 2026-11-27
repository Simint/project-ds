using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 계절별 기온이 체온에 주는 영향 (docs/design/GAME_DESIGN.md 5.6 "계절 기온은 실내에도 적용"). 시간당 체온 변화에
    /// 더하는 값이다(음수면 더 빨리 식는다). 실제 값은 비공개 Data/World/ 에셋에만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Season Settings")]
    public sealed class SeasonSettings : ScriptableObject
    {
        [SerializeField] private float springTemperaturePerHour = 0f;
        [SerializeField] private float summerTemperaturePerHour = 0f;
        [SerializeField] private float autumnTemperaturePerHour = 0f;
        [SerializeField] private float winterTemperaturePerHour = 0f;

        public float TemperaturePerHour(Season season) => season switch
        {
            Season.Summer => summerTemperaturePerHour,
            Season.Autumn => autumnTemperaturePerHour,
            Season.Winter => winterTemperaturePerHour,
            _ => springTemperaturePerHour
        };
    }
}
