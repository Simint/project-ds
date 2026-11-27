using Game.World;
using NUnit.Framework;
using UnityEngine;

/// <summary>낮밤 단계와 계절 순환 계산을 확인한다 (docs/design/GAME_DESIGN.md 5.1, 5.6). 수치는 임의 값이다.</summary>
public sealed class DayNightAndSeasonTests
{
    private const int Rise = 250;
    private const int Set = 1200;

    [Test]
    public void IsNight_BeforeSunriseAndAfterSunset()
    {
        Assert.IsTrue(DayNightMath.IsNight(249, Rise, Set));
        Assert.IsFalse(DayNightMath.IsNight(250, Rise, Set));
        Assert.IsFalse(DayNightMath.IsNight(1199, Rise, Set));
        Assert.IsTrue(DayNightMath.IsNight(1200, Rise, Set));
    }

    [Test]
    public void Daylight_RampsAroundSunriseAndSunset()
    {
        Assert.AreEqual(0f, DayNightMath.Daylight(100, Rise, Set, 40), 0.0001f);
        Assert.AreEqual(0.5f, DayNightMath.Daylight(Rise, Rise, Set, 40), 0.0001f);
        Assert.AreEqual(1f, DayNightMath.Daylight(700, Rise, Set, 40), 0.0001f);
        Assert.AreEqual(0.5f, DayNightMath.Daylight(Set, Rise, Set, 40), 0.0001f);
        Assert.AreEqual(0f, DayNightMath.Daylight(1300, Rise, Set, 40), 0.0001f);
    }

    [Test]
    public void SunAndMoonProgress_CoverDayAndNight()
    {
        Assert.AreEqual(0f, DayNightMath.SunProgress(Rise, Rise, Set), 0.0001f);
        Assert.AreEqual(0.5f, DayNightMath.SunProgress(725, Rise, Set), 0.0001f);
        Assert.AreEqual(-1f, DayNightMath.SunProgress(100, Rise, Set));
        Assert.AreEqual(0f, DayNightMath.MoonProgress(Set, Rise, Set), 0.0001f);
        Assert.AreEqual(-1f, DayNightMath.MoonProgress(700, Rise, Set));
        // 밤 길이 490분: 자정을 넘긴 0시는 해 진 뒤 240분.
        Assert.AreEqual(240f / 490f, DayNightMath.MoonProgress(0, Rise, Set), 0.0001f);
    }

    [Test]
    public void ArcPosition_EndsAtBase_PeaksInMiddle()
    {
        Vector2 start = DayNightMath.ArcPosition(0f, 8f, 1f, 5f);
        Vector2 mid = DayNightMath.ArcPosition(0.5f, 8f, 1f, 5f);
        Assert.AreEqual(-8f, start.x, 0.0001f);
        Assert.AreEqual(1f, start.y, 0.0001f);
        Assert.AreEqual(0f, mid.x, 0.0001f);
        Assert.AreEqual(5f, mid.y, 0.0001f);
    }

    [Test]
    public void MinuteOfDay_Wraps()
    {
        Assert.AreEqual(10, DayNightMath.MinuteOfDay(WorldClock.MinutesPerDay * 3 + 10));
    }

    [Test]
    public void SeasonAt_CyclesEveryLength()
    {
        long day = WorldClock.MinutesPerDay;
        Assert.AreEqual(Season.Spring, SeasonMath.SeasonAt(0, 7));
        Assert.AreEqual(Season.Spring, SeasonMath.SeasonAt(day * 6, 7));
        Assert.AreEqual(Season.Summer, SeasonMath.SeasonAt(day * 7, 7));
        Assert.AreEqual(Season.Winter, SeasonMath.SeasonAt(day * 21, 7));
        Assert.AreEqual(Season.Spring, SeasonMath.SeasonAt(day * 28, 7));
        Assert.AreEqual(3, SeasonMath.DayOfSeason(day * 10, 7));
    }

    [Test]
    public void SeasonAt_ZeroLength_StaysSpring()
    {
        Assert.AreEqual(Season.Spring, SeasonMath.SeasonAt(WorldClock.MinutesPerDay * 100, 0));
    }

    [Test]
    public void SeasonSystem_RaisesChangedWhenDayCrossesSeason()
    {
        long day = WorldClock.MinutesPerDay;
        var clock = new FakeWorldClock(day * 2 - 1, new WorldSettings("x", 2, 60f));
        using var seasons = new SeasonSystem(clock);
        Season? changed = null;
        seasons.SeasonChanged += s => changed = s;

        clock.Tick(1);

        Assert.AreEqual(Season.Summer, seasons.Current);
        Assert.AreEqual(Season.Summer, changed);
    }

    [Test]
    public void SeasonSystem_Advance_JumpsToWinter()
    {
        var clock = new FakeWorldClock(0, new WorldSettings("x", 3, 60f));
        using var seasons = new SeasonSystem(clock);
        clock.Advance(WorldClock.MinutesPerDay * 9);
        Assert.IsTrue(seasons.IsWinter);
    }
}
