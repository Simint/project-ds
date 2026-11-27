using System;

namespace Game.World
{
    /// <summary>
    /// 현재 계절 서비스 (ARCHITECTURE.md 4.1 SeasonSystem). 시계와 월드 설정(계절 길이)에서 계산하고, 날이 바뀔 때
    /// 계절이 넘어가면 알린다. 순수 C# 클래스라 가짜 시계로 테스트한다.
    /// </summary>
    public sealed class SeasonSystem : IDisposable
    {
        private readonly IWorldClock clock;
        private readonly SeasonSettings settings;

        public Season Current { get; private set; }
        public bool IsWinter => Current == Season.Winter;

        /// <summary>지금 계절 기온이 시간당 체온 변화에 더하는 값(실내에도 적용, 5.6).</summary>
        public float TemperaturePerHour => settings != null ? settings.TemperaturePerHour(Current) : 0f;

        public event Action<Season> SeasonChanged;

        public SeasonSystem(IWorldClock clock, SeasonSettings settings = null)
        {
            this.clock = clock;
            this.settings = settings;
            Current = Compute();
            if (clock == null) return;
            clock.DayChanged += Refresh;
            clock.Advanced += HandleAdvanced;
        }

        public void Dispose()
        {
            if (clock == null) return;
            clock.DayChanged -= Refresh;
            clock.Advanced -= HandleAdvanced;
        }

        /// <summary>시각이나 월드 설정을 직접 바꾼 뒤(불러오기·새 게임) 부른다.</summary>
        public void Refresh()
        {
            Season next = Compute();
            if (next == Current) return;
            Current = next;
            SeasonChanged?.Invoke(next);
        }

        private void HandleAdvanced(long minutes) => Refresh();

        private Season Compute() =>
            clock == null ? Season.Spring : SeasonMath.SeasonAt(clock.TotalMinutes, clock.Settings != null ? clock.Settings.SeasonLengthDays : 0);
    }
}
