using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.World
{
    /// <summary>
    /// 낮밤 표현 (ARCHITECTURE.md 4.1 DayNightCycle, docs/design/GAME_DESIGN.md 5.6). WorldClock 시각으로 Global Light 2D의
    /// 밝기·색과 하늘색(카메라 배경)을 바꾸고, Sky 레이어의 해·달을 카메라 기준 호 위에 띄운다. 계산은 DayNightMath가
    /// 맡고 여기는 표시만 한다. 시계가 1분 지날 때마다(또는 건너뛸 때) 다시 계산한다. 해·달은 카메라 자식으로 두어
    /// Cinemachine이 카메라를 옮긴 뒤에도 한 프레임 늦지 않게 따라간다(로컬 위치만 분마다 바꾼다).
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour, IDayNight
    {
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private DayNightDefinition definition;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer sun;
        [SerializeField] private SpriteRenderer moon;

        private float sunProgress = -1f;
        private float moonProgress = -1f;
        private bool initialized;

        public bool IsNight { get; private set; }
        public float Daylight { get; private set; } = 1f;

        public event Action<bool> NightChanged;

        private void Awake()
        {
            if (worldClock == null) Debug.LogError("[DayNightCycle] worldClock 참조가 비어 있습니다.", this);
            if (definition == null) Debug.LogError("[DayNightCycle] definition 참조가 비어 있습니다.", this);
            if (globalLight == null) Debug.LogError("[DayNightCycle] globalLight 참조가 비어 있습니다.", this);
            if (targetCamera == null) Debug.LogError("[DayNightCycle] targetCamera 참조가 비어 있습니다.", this);
            if (sun == null) Debug.LogError("[DayNightCycle] sun 참조가 비어 있습니다.", this);
            if (moon == null) Debug.LogError("[DayNightCycle] moon 참조가 비어 있습니다.", this);
            if (targetCamera == null) return;
            if (sun != null) sun.transform.SetParent(targetCamera.transform, false);
            if (moon != null) moon.transform.SetParent(targetCamera.transform, false);
        }

        private void OnEnable()
        {
            if (worldClock == null) return;
            worldClock.MinutePassed += Refresh;
            worldClock.Advanced += HandleAdvanced;
        }

        private void OnDisable()
        {
            if (worldClock == null) return;
            worldClock.MinutePassed -= Refresh;
            worldClock.Advanced -= HandleAdvanced;
        }

        private void Start() => Refresh();

        private void HandleAdvanced(long minutes) => Refresh();

        /// <summary>시각을 직접 바꾼 뒤(불러오기·새 게임) 바로 반영할 때도 부른다.</summary>
        public void Refresh()
        {
            if (worldClock == null || definition == null) return;
            int minute = DayNightMath.MinuteOfDay(worldClock.TotalMinutes);
            int rise = definition.SunriseMinute;
            int set = definition.SunsetMinute;

            Daylight = DayNightMath.Daylight(minute, rise, set, definition.TransitionMinutes);
            sunProgress = DayNightMath.SunProgress(minute, rise, set);
            moonProgress = DayNightMath.MoonProgress(minute, rise, set);

            if (globalLight != null)
            {
                globalLight.intensity = Mathf.Lerp(definition.NightIntensity, definition.DayIntensity, Daylight);
                globalLight.color = Color.Lerp(definition.NightLightColor, definition.DayLightColor, Daylight);
            }
            if (targetCamera != null)
                targetCamera.backgroundColor = Color.Lerp(definition.NightSkyColor, definition.DaySkyColor, Daylight);
            if (sun != null) sun.enabled = sunProgress >= 0f;
            if (moon != null) moon.enabled = moonProgress >= 0f;
            Place(sun, sunProgress);
            Place(moon, moonProgress);

            bool night = DayNightMath.IsNight(minute, rise, set);
            if (!initialized || night != IsNight)
            {
                bool changed = initialized;
                initialized = true;
                IsNight = night;
                if (changed) NightChanged?.Invoke(night);
            }
        }

        /// <summary>카메라 기준 호 위의 로컬 위치로. z는 카메라 앞(월드 z 0)에 오도록 카메라 z만큼 되돌린다.</summary>
        private void Place(SpriteRenderer body, float progress)
        {
            if (body == null || progress < 0f || definition == null || targetCamera == null) return;
            Vector2 offset = DayNightMath.ArcPosition(progress, definition.ArcHalfWidthTiles, definition.ArcBaseHeightTiles, definition.ArcPeakHeightTiles);
            body.transform.localPosition = new Vector3(offset.x, offset.y, -targetCamera.transform.position.z);
        }
    }
}
