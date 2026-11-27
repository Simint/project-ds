using System;

namespace Game.World
{
    /// <summary>
    /// 지금 날씨 서비스 (ARCHITECTURE.md 4.1 WeatherSystem, docs/design/GAME_DESIGN.md 5.6). 계절별 가중치로 날씨를 뽑고,
    /// 끝나는 시각이 되면 다음 날씨를 뽑는다. 시간을 건너뛰면(맵 선택 이동·잠) 그 사이의 날씨를 한 번에 넘긴다.
    /// 계절이 바뀌었는데 지금 날씨가 새 계절에 나올 수 없으면(봄의 눈 등) 바로 다시 뽑는다.
    /// 빗물 통이 차는 날씨가 이어진 시간(분)은 RainMinutesTotal에 쌓는다 — 은신처에 없을 때 내린 비와 건너뛴 시간의
    /// 비를 빗물 통이 나중에 차이만큼 받는다. 지금 날씨 id·끝나는 시각·누적 비 시간은 저장된다. 순수 C# 클래스다.
    /// </summary>
    public sealed class WeatherSystem : IDisposable
    {
        // 시간을 아주 길게 건너뛸 때 무한히 돌지 않게 하는 상한(기술 값).
        private const int MaxRollsPerAdvance = 256;

        private readonly IWorldClock clock;
        private readonly SeasonSystem seasons;
        private readonly WeatherSettings settings;
        private readonly Random random;
        private long accountedUntilMinute;

        public WeatherDefinition Current { get; private set; }
        public long EndsAtMinute { get; private set; }

        /// <summary>지금까지 빗물 통이 차는 날씨(비·폭우)가 이어진 게임 분의 합.</summary>
        public long RainMinutesTotal { get; private set; }

        public event Action<WeatherDefinition> WeatherChanged;

        public WeatherSystem(IWorldClock clock, SeasonSystem seasons, WeatherSettings settings, int seed)
        {
            this.clock = clock;
            this.seasons = seasons;
            this.settings = settings;
            random = new Random(seed);
            if (seasons != null) seasons.SeasonChanged += HandleSeasonChanged;
            if (clock == null) return;
            clock.MinutePassed += HandleMinutePassed;
            clock.Advanced += HandleAdvanced;
        }

        public void Dispose()
        {
            if (seasons != null) seasons.SeasonChanged -= HandleSeasonChanged;
            if (clock == null) return;
            clock.MinutePassed -= HandleMinutePassed;
            clock.Advanced -= HandleAdvanced;
        }

        private long Now => clock != null ? clock.TotalMinutes : 0;

        /// <summary>새 게임: 지금 시각부터 새로 뽑는다.</summary>
        public void Begin()
        {
            RainMinutesTotal = 0;
            accountedUntilMinute = Now;
            Roll(Now);
        }

        /// <summary>불러오기: 저장된 날씨·끝나는 시각·누적 비 시간을 그대로 쓴다. 날씨를 못 찾으면 새로 뽑는다.</summary>
        public void Restore(string weatherId, long endsAtMinute, long rainMinutesTotal = 0)
        {
            RainMinutesTotal = Math.Max(0, rainMinutesTotal);
            accountedUntilMinute = Now;
            WeatherDefinition saved = settings != null ? settings.Find(weatherId) : null;
            if (saved == null)
            {
                Roll(Now);
                return;
            }
            Current = saved;
            EndsAtMinute = endsAtMinute;
            WeatherChanged?.Invoke(Current);
            CatchUp();
        }

        private void HandleMinutePassed()
        {
            if (clock == null) return;
            if (clock.TotalMinutes >= EndsAtMinute)
            {
                Account(EndsAtMinute);
                Roll(EndsAtMinute);
            }
            Account(clock.TotalMinutes);
        }

        private void HandleAdvanced(long minutes) => CatchUp();

        private void CatchUp()
        {
            if (clock == null) return;
            for (int i = 0; i < MaxRollsPerAdvance && clock.TotalMinutes >= EndsAtMinute; i++)
            {
                Account(EndsAtMinute);
                Roll(EndsAtMinute);
            }
            if (clock.TotalMinutes >= EndsAtMinute)
            {
                Account(clock.TotalMinutes);
                Roll(clock.TotalMinutes);
            }
            Account(clock.TotalMinutes);
        }

        private void HandleSeasonChanged(Season season)
        {
            if (Current == null || settings == null || Current == settings.Fallback) return;
            if (WeatherRules.WeightOf(settings.Chances, Current, season) > 0f) return;
            Account(Now);
            Roll(Now);
        }

        /// <summary>지난번에 센 시각부터 upTo까지, 지금 날씨가 빗물 통을 채우면 그 시간을 더한다.</summary>
        private void Account(long upTo)
        {
            if (upTo <= accountedUntilMinute) return;
            if (Current != null && Current.FillsRainBarrels) RainMinutesTotal += upTo - accountedUntilMinute;
            accountedUntilMinute = upTo;
        }

        private void Roll(long startMinute)
        {
            Season season = seasons != null ? seasons.Current : Season.Spring;
            WeatherDefinition next = settings != null ? WeatherRules.Pick(settings.Chances, season, random.NextDouble()) : null;
            if (next == null && settings != null) next = settings.Fallback;
            Current = next;
            EndsAtMinute = startMinute + WeatherRules.Duration(next, random.NextDouble());
            WeatherChanged?.Invoke(Current);
        }
    }
}
