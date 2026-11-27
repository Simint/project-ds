using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 낮밤 표현 수치 (docs/design/GAME_DESIGN.md 5.1, 5.6). 해 뜨고 지는 시각(하루 중 분),
    /// 밝기·색, 해·달이 그리는 호. 실제 값은 비공개 Data/World/ 에셋에만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Day Night Definition")]
    public sealed class DayNightDefinition : ScriptableObject
    {
        [Header("시각 (하루 중 분, 0~1439)")]
        [SerializeField] [Range(0, 1439)] private int sunriseMinute = 0;
        [SerializeField] [Range(0, 1439)] private int sunsetMinute = 0;
        [Tooltip("해 뜰 녘·질 녘에 밝기가 바뀌는 시간(분).")]
        [SerializeField] [Min(0)] private int transitionMinutes = 0;

        [Header("전체 조명 (Global Light 2D)")]
        [SerializeField] [Min(0f)] private float dayIntensity = 1f;
        [SerializeField] [Min(0f)] private float nightIntensity = 1f;
        [SerializeField] private Color dayLightColor = Color.white;
        [SerializeField] private Color nightLightColor = Color.white;

        [Header("하늘 (카메라 배경색)")]
        [SerializeField] private Color daySkyColor = Color.white;
        [SerializeField] private Color nightSkyColor = Color.white;

        [Header("해·달의 호 (카메라 기준, 타일)")]
        [SerializeField] private float arcHalfWidthTiles = 0f;
        [SerializeField] private float arcBaseHeightTiles = 0f;
        [SerializeField] private float arcPeakHeightTiles = 0f;

        public int SunriseMinute => sunriseMinute;
        public int SunsetMinute => sunsetMinute;
        public int TransitionMinutes => transitionMinutes;
        public float DayIntensity => dayIntensity;
        public float NightIntensity => nightIntensity;
        public Color DayLightColor => dayLightColor;
        public Color NightLightColor => nightLightColor;
        public Color DaySkyColor => daySkyColor;
        public Color NightSkyColor => nightSkyColor;
        public float ArcHalfWidthTiles => arcHalfWidthTiles;
        public float ArcBaseHeightTiles => arcBaseHeightTiles;
        public float ArcPeakHeightTiles => arcPeakHeightTiles;
    }
}
