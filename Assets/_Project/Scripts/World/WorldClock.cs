using System;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Boot 씬에 하나. <see cref="IWorldClock"/> 구현. 생존·부패·작물은 Time.deltaTime이 아니라
    /// 이 클래스가 쌓는 게임 내 분(minute)을 기준으로 계산한다 (ARCHITECTURE.md 2.5, 4.1).
    /// GameBootstrap이 Awake에서 저장된 시각으로 덮어쓰므로, 그보다 먼저 기본값을 정해 둬야 한다
    /// (Unity는 서로 다른 오브젝트의 Awake 순서를 보장하지 않는다).
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
    }
}
