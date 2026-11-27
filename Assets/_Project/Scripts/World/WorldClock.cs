using System;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Boot 씬에 하나. <see cref="IWorldClock"/> 구현. 생존·부패·작물은 Time.deltaTime이 아니라
    /// 이 클래스가 쌓는 게임 내 분(minute)을 기준으로 계산한다 (ARCHITECTURE.md 2.5, 4.1).
    /// 새 게임·이어하기 때 GameBootstrap이 시각과 설정을 덮어쓰므로, 그보다 먼저(Awake에서) 기본값을 정해 둔다
    /// (Unity는 서로 다른 오브젝트의 Awake 순서를 보장하지 않는다). 타이틀 화면 동안은 IsPaused로 멈춘다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class WorldClock : MonoBehaviour, IWorldClock
    {
        /// <summary>하루는 항상 게임 내 1440분(24시간)이다. 바꿀 수 있는 건 하루가 현실로 몇 초인지뿐이다.</summary>
        public const int MinutesPerDay = 1440;

        [SerializeField] private WorldClockDefaults defaults;
        [SerializeField] private WorldSettings settings = new WorldSettings();
        [SerializeField] private long startMinutes = 0;

        private long totalMinutes;
        private float minuteAccumulator;

        public long TotalMinutes => totalMinutes;
        public WorldSettings Settings => settings;

        /// <summary>타이틀 화면처럼 게임이 시작되기 전에는 시간이 흐르지 않는다.</summary>
        public bool IsPaused { get; set; }

        /// <summary>새 월드가 시작되는 시각(분). 새 게임에서 쓴다.</summary>
        public long DefaultStartMinutes => defaults != null ? defaults.StartMinutes : 0;

        /// <summary>하루 현실 길이 기본값(초). 저장 파일에 값이 없을 때 쓴다.</summary>
        public float DefaultRealSecondsPerDay => defaults != null ? defaults.RealSecondsPerDay : 0f;

        /// <summary>새 게임 화면의 계절 길이 선택지와 기본값(일).</summary>
        public System.Collections.Generic.IReadOnlyList<int> SeasonLengthOptionsDays =>
            defaults != null ? defaults.SeasonLengthOptionsDays : System.Array.Empty<int>();
        public int DefaultSeasonLengthDays => defaults != null ? defaults.SeasonLengthDays : 0;

        public event Action MinutePassed;
        public event Action HourPassed;
        public event Action DayChanged;
        public event Action<long> Advanced;

        private void Awake()
        {
            if (defaults == null) Debug.LogError("[WorldClock] defaults 참조가 비어 있습니다.", this);
            else
            {
                settings = new WorldSettings(defaults.DifficultyId, defaults.SeasonLengthDays, defaults.RealSecondsPerDay);
                startMinutes = defaults.StartMinutes;
            }
            totalMinutes = startMinutes;
        }

        private void Update()
        {
            // defaults가 비어 있으면 settings.RealSecondsPerDay가 0으로 남는다 — 나누기 전에 걸러서
            // Infinity/NaN이 minuteAccumulator로 퍼지지 않게 한다.
            if (IsPaused || settings.RealSecondsPerDay <= 0f) return;

            float gameMinutesPerRealSecond = MinutesPerDay / settings.RealSecondsPerDay;
            minuteAccumulator += Time.deltaTime * gameMinutesPerRealSecond;
            if (minuteAccumulator < 1f) return;

            int wholeMinutes = (int)minuteAccumulator;
            minuteAccumulator -= wholeMinutes;
            for (int i = 0; i < wholeMinutes; i++)
                TickOneMinute();
        }

        private void TickOneMinute()
        {
            long previousHour = totalMinutes / 60;
            long previousDay = totalMinutes / MinutesPerDay;
            totalMinutes++;
            MinutePassed?.Invoke();
            if (totalMinutes / 60 != previousHour) HourPassed?.Invoke();
            if (totalMinutes / MinutesPerDay != previousDay) DayChanged?.Invoke();
        }

        public void Advance(long minutes)
        {
            if (minutes <= 0) return;
            long previousDay = totalMinutes / MinutesPerDay;
            totalMinutes += minutes;
            Advanced?.Invoke(minutes);
            if (totalMinutes / MinutesPerDay != previousDay) DayChanged?.Invoke();
        }

        public void SetTotalMinutes(long minutes) => totalMinutes = minutes;

        /// <summary>새 게임·불러오기에서 월드 설정(난이도·계절 길이)을 바꾼다. 하루 현실 길이가 비어 있으면 기본값을 쓴다.</summary>
        public void ApplySettings(WorldSettings value)
        {
            if (value == null) return;
            float realSeconds = value.RealSecondsPerDay > 0f ? value.RealSecondsPerDay : DefaultRealSecondsPerDay;
            int seasonDays = value.SeasonLengthDays > 0 ? value.SeasonLengthDays : settings.SeasonLengthDays;
            settings = new WorldSettings(value.DifficultyId, seasonDays, realSeconds);
            minuteAccumulator = 0f;
        }
    }
}
