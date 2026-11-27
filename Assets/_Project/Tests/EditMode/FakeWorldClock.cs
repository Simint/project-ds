using System;
using Game.World;

/// <summary>테스트용 가짜 시계 (csharp.md "시간은 IWorldClock으로 받아 테스트에서 가짜 시계로 바꾼다").</summary>
public sealed class FakeWorldClock : IWorldClock
{
    private long totalMinutes;

    public FakeWorldClock(long startMinutes, WorldSettings settings)
    {
        totalMinutes = startMinutes;
        Settings = settings;
    }

    public long TotalMinutes => totalMinutes;
    public WorldSettings Settings { get; }

    public event Action MinutePassed;
    public event Action HourPassed;
    public event Action DayChanged;
    public event Action<long> Advanced;

    /// <summary>실시간처럼 1분씩 흘린다(분·시·날 이벤트 포함).</summary>
    public void Tick(int minutes)
    {
        for (int i = 0; i < minutes; i++)
        {
            long hour = totalMinutes / 60;
            long day = totalMinutes / WorldClock.MinutesPerDay;
            totalMinutes++;
            MinutePassed?.Invoke();
            if (totalMinutes / 60 != hour) HourPassed?.Invoke();
            if (totalMinutes / WorldClock.MinutesPerDay != day) DayChanged?.Invoke();
        }
    }

    public void Advance(long minutes)
    {
        if (minutes <= 0) return;
        totalMinutes += minutes;
        Advanced?.Invoke(minutes);
    }

    public void SetTotalMinutes(long minutes) => totalMinutes = minutes;
}
