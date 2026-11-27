using System.Collections.Generic;
using Game.AI;
using Game.Building;
using Game.Items;
using Game.Survival;
using Game.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 날씨 뽑기·지속·효과, 날씨 서비스의 넘김·불러오기, 난이도 배율, 사망 주머니 범위, 밤 좀비 보정, 빗물 통 채우기를 확인한다
/// (docs/design/GAME_DESIGN.md 5.6의 8단계 범위, 5.8). 수치는 임의 값이다.
/// </summary>
public sealed class WeatherAndDifficultyTests
{
    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    private WeatherDefinition Weather(string id, WeatherKind kind, int min, int max, float temp = 0f, float sight = 1f, float enemySight = 1f, float noise = 1f)
    {
        var w = ScriptableObject.CreateInstance<WeatherDefinition>();
        created.Add(w);
        var so = new SerializedObject(w);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("minDurationMinutes").intValue = min;
        so.FindProperty("maxDurationMinutes").intValue = max;
        so.FindProperty("temperaturePerHour").floatValue = temp;
        so.FindProperty("playerSightMultiplier").floatValue = sight;
        so.FindProperty("enemySightMultiplier").floatValue = enemySight;
        so.FindProperty("noiseRadiusMultiplier").floatValue = noise;
        so.ApplyModifiedPropertiesWithoutUndo();
        return w;
    }

    private T Make<T>(System.Action<SerializedObject> setup) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        created.Add(asset);
        var so = new SerializedObject(asset);
        setup(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    [Test]
    public void Pick_UsesSeasonWeights_AndSkipsZeroWeight()
    {
        var clear = Weather("clear", WeatherKind.Clear, 10, 10);
        var snow = Weather("snow", WeatherKind.Snow, 10, 10);
        var chances = new List<WeatherChance> { new WeatherChance(clear, 1, 1, 1, 1), new WeatherChance(snow, 0, 0, 0, 3) };

        Assert.AreSame(clear, WeatherRules.Pick(chances, Season.Summer, 0.99));
        Assert.AreSame(clear, WeatherRules.Pick(chances, Season.Winter, 0.2));
        Assert.AreSame(snow, WeatherRules.Pick(chances, Season.Winter, 0.3));
        Assert.IsNull(WeatherRules.Pick(new List<WeatherChance>(), Season.Spring, 0.5));
    }

    [Test]
    public void Duration_StaysWithinRange_AndAtLeastOneMinute()
    {
        var w = Weather("rain", WeatherKind.Rain, 30, 90);
        Assert.AreEqual(30, WeatherRules.Duration(w, 0.0));
        Assert.AreEqual(90, WeatherRules.Duration(w, 0.9999));
        Assert.AreEqual(1, WeatherRules.Duration(Weather("zero", WeatherKind.Clear, 0, 0), 0.5));
    }

    [Test]
    public void Effects_AreIgnoredIndoors()
    {
        var fog = Weather("fog", WeatherKind.Fog, 10, 10, temp: -2f, sight: 0.4f, enemySight: 0.35f, noise: 0.6f);
        Assert.AreEqual(-2f, WeatherRules.TemperaturePerHour(fog, false));
        Assert.AreEqual(0f, WeatherRules.TemperaturePerHour(fog, true));
        Assert.AreEqual(0.35f, WeatherRules.EnemySightMultiplier(fog, false));
        Assert.AreEqual(1f, WeatherRules.EnemySightMultiplier(fog, true));
        Assert.AreEqual(0.6f, WeatherRules.NoiseRadiusMultiplier(fog, false));
        Assert.AreEqual(1f, WeatherRules.PlayerSightMultiplier(fog, true));
    }

    [Test]
    public void WeatherSystem_RollsNextWhenCurrentEnds_AndCatchesUpOnAdvance()
    {
        var clear = Weather("clear", WeatherKind.Clear, 10, 10);
        var settings = Make<WeatherSettings>(so => so.FindProperty("fallback").objectReferenceValue = clear);
        var clock = new FakeWorldClock(0, new WorldSettings("x", 5, 60f));
        using var seasons = new SeasonSystem(clock);
        using var weather = new WeatherSystem(clock, seasons, settings, 1);
        int changes = 0;
        weather.WeatherChanged += _ => changes++;

        weather.Begin();
        Assert.AreSame(clear, weather.Current);
        Assert.AreEqual(10, weather.EndsAtMinute);

        clock.Tick(10);
        Assert.AreEqual(20, weather.EndsAtMinute);

        clock.Advance(35);
        Assert.Greater(weather.EndsAtMinute, clock.TotalMinutes);
        Assert.AreEqual(50, weather.EndsAtMinute);
        Assert.GreaterOrEqual(changes, 5);
    }

    [Test]
    public void WeatherSystem_Restore_KeepsSavedWeatherAndEnd()
    {
        var clear = Weather("clear", WeatherKind.Clear, 10, 10);
        var rain = Weather("rain", WeatherKind.Rain, 10, 10);
        var settings = Make<WeatherSettings>(so =>
        {
            so.FindProperty("fallback").objectReferenceValue = clear;
            SerializedProperty list = so.FindProperty("chances");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("weather").objectReferenceValue = rain;
        });
        var clock = new FakeWorldClock(100, new WorldSettings("x", 5, 60f));
        using var seasons = new SeasonSystem(clock);
        using var weather = new WeatherSystem(clock, seasons, settings, 1);

        weather.Restore("rain", 130);

        Assert.AreSame(rain, weather.Current);
        Assert.AreEqual(130, weather.EndsAtMinute);
    }

    [Test]
    public void DifficultyRules_ScaleAndClamp()
    {
        var hard = Make<DifficultyDefinition>(so =>
        {
            so.FindProperty("enemyHealthMultiplier").floatValue = 1.5f;
            so.FindProperty("enemyDamageMultiplier").floatValue = 2f;
            so.FindProperty("lootMultiplier").floatValue = 0.3f;
            so.FindProperty("respawnTimeMultiplier").floatValue = 0.5f;
            so.FindProperty("survivalDecayMultiplier").floatValue = 1.7f;
        });
        Assert.AreEqual(15f, DifficultyRules.EnemyHealth(10f, hard), 0.0001f);
        Assert.AreEqual(6f, DifficultyRules.EnemyDamage(3f, hard), 0.0001f);
        Assert.AreEqual(1, DifficultyRules.LootCount(2, hard));
        Assert.AreEqual(3, DifficultyRules.LootCount(10, hard));
        Assert.AreEqual(0, DifficultyRules.LootCount(0, hard));
        Assert.AreEqual(WorldClock.MinutesPerDay, DifficultyRules.RespawnMinutes(2, hard));
        Assert.AreEqual(0, DifficultyRules.RespawnMinutes(0, hard));
        Assert.AreEqual(1.7f, DifficultyRules.SurvivalDecay(hard), 0.0001f);
        Assert.AreEqual(4, DifficultyRules.LootCount(4, null));
        Assert.AreEqual(DeathDropScope.Bag, DifficultyRules.DropScope(null));
    }

    [Test]
    public void DeathDropRules_EasyDropsMaterialsOnly()
    {
        Assert.IsTrue(DeathDropRules.ShouldDrop(ItemCategory.Material, DeathDropScope.MaterialsOnly));
        Assert.IsFalse(DeathDropRules.ShouldDrop(ItemCategory.Food, DeathDropScope.MaterialsOnly));
        Assert.IsTrue(DeathDropRules.ShouldDrop(ItemCategory.Food, DeathDropScope.Bag));
        Assert.IsTrue(DeathDropRules.ShouldDrop(ItemCategory.Tool, DeathDropScope.BagAndEquipment));
        Assert.IsFalse(DeathDropRules.DropsEquipment(DeathDropScope.Bag));
        Assert.IsTrue(DeathDropRules.DropsEquipment(DeathDropScope.BagAndEquipment));
    }

    [Test]
    public void CreatureStatRules_NightBoostsOnlyAtNight_AndStacksWithWeatherAndDifficulty()
    {
        var zombie = Make<CreatureDefinition>(so =>
        {
            so.FindProperty("moveSpeedTiles").floatValue = 2f;
            so.FindProperty("sightRangeTiles").floatValue = 4f;
            so.FindProperty("attackDamage").floatValue = 3f;
            so.FindProperty("maxHealth").floatValue = 10f;
            so.FindProperty("nightSpeedMultiplier").floatValue = 1.5f;
            so.FindProperty("nightDamageMultiplier").floatValue = 2f;
            so.FindProperty("nightSightMultiplier").floatValue = 1.25f;
        });
        var hard = Make<DifficultyDefinition>(so =>
        {
            so.FindProperty("enemyDamageMultiplier").floatValue = 1.5f;
            so.FindProperty("enemyHealthMultiplier").floatValue = 2f;
        });

        Assert.AreEqual(2f, CreatureStatRules.MoveSpeed(zombie, false), 0.0001f);
        Assert.AreEqual(3f, CreatureStatRules.MoveSpeed(zombie, true), 0.0001f);
        Assert.AreEqual(9f, CreatureStatRules.AttackDamage(zombie, true, hard), 0.0001f);
        Assert.AreEqual(2.5f, CreatureStatRules.SightRange(zombie, true, 0.5f), 0.0001f);
        Assert.AreEqual(20f, CreatureStatRules.MaxHealth(zombie, hard), 0.0001f);
    }

    private WeatherDefinition RainLike(string id, int min, int max)
    {
        var w = Weather(id, WeatherKind.Rain, min, max);
        var so = new SerializedObject(w);
        so.FindProperty("fillsRainBarrels").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return w;
    }

    [Test]
    public void WeatherSystem_CountsRainMinutes_IncludingAdvance()
    {
        var rain = RainLike("drizzle", 1000, 1000);
        var settings = Make<WeatherSettings>(so => so.FindProperty("fallback").objectReferenceValue = rain);
        var clock = new FakeWorldClock(0, new WorldSettings("x", 5, 60f));
        using var seasons = new SeasonSystem(clock);
        using var weather = new WeatherSystem(clock, seasons, settings, 3);
        weather.Begin();

        clock.Tick(7);
        clock.Advance(33);

        Assert.AreEqual(40, weather.RainMinutesTotal);
    }

    [Test]
    public void WeatherSystem_Restore_KeepsRainTotal_AndDryWeatherAddsNothing()
    {
        var dry = Weather("dry", WeatherKind.Clear, 500, 500);
        var settings = Make<WeatherSettings>(so => so.FindProperty("fallback").objectReferenceValue = dry);
        var clock = new FakeWorldClock(10, new WorldSettings("x", 5, 60f));
        using var seasons = new SeasonSystem(clock);
        using var weather = new WeatherSystem(clock, seasons, settings, 3);

        weather.Restore("dry", 400, 77);
        clock.Tick(20);

        Assert.AreEqual(77, weather.RainMinutesTotal);
    }

    [Test]
    public void WeatherSystem_SeasonChange_RerollsWeatherNotAllowedInNewSeason()
    {
        var clear = Weather("clear2", WeatherKind.Clear, 5000, 5000);
        var snow = Weather("snow2", WeatherKind.Snow, 5000, 5000);
        var settings = Make<WeatherSettings>(so =>
        {
            so.FindProperty("fallback").objectReferenceValue = clear;
            SerializedProperty list = so.FindProperty("chances");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("weather").objectReferenceValue = clear;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("springWeight").floatValue = 1f;
            list.GetArrayElementAtIndex(1).FindPropertyRelative("weather").objectReferenceValue = snow;
            list.GetArrayElementAtIndex(1).FindPropertyRelative("winterWeight").floatValue = 1f;
        });
        long day = WorldClock.MinutesPerDay;
        // 계절 1일 → 3일째가 겨울, 4일째가 봄.
        var clock = new FakeWorldClock(day * 4 - 1, new WorldSettings("x", 1, 60f));
        using var seasons = new SeasonSystem(clock);
        using var weather = new WeatherSystem(clock, seasons, settings, 3);
        weather.Restore("snow2", day * 10);
        Assert.AreSame(snow, weather.Current);

        clock.Tick(1);

        Assert.AreEqual(Season.Spring, seasons.Current);
        Assert.AreSame(clear, weather.Current);
    }

    [Test]
    public void OverlayAlpha_TakesDenserOfColorAndSight_ZeroIndoors()
    {
        var fog = Weather("fog2", WeatherKind.Fog, 10, 10, sight: 0.3f);
        Assert.AreEqual(0.7f, WeatherRules.OverlayAlpha(fog, false), 0.0001f);
        Assert.AreEqual(0f, WeatherRules.OverlayAlpha(fog, true));
        Assert.AreEqual(0f, WeatherRules.OverlayAlpha(null, false));
    }

    [Test]
    public void NoiseSystem_UsesProvidedMultiplier()
    {
        float heard = 0f;
        var listener = new RecordingListener(r => heard = r);
        var noise = new Game.Combat.NoiseSystem(() => 0.25f);
        noise.Register(listener);
        noise.Emit(Vector2.zero, 8f, Game.Combat.NoiseKind.Footstep);
        Assert.AreEqual(2f, heard, 0.0001f);
        Assert.AreEqual(1f, new Game.Combat.NoiseSystem().RadiusMultiplier);
    }

    private sealed class RecordingListener : Game.Combat.INoiseListener
    {
        private readonly System.Action<float> onHeard;
        public RecordingListener(System.Action<float> onHeard) => this.onHeard = onHeard;
        public void OnNoiseHeard(Vector2 position, float radius, Game.Combat.NoiseKind kind) => onHeard(radius);
    }

    [Test]
    public void LootCount_HalfAlwaysRoundsUp()
    {
        var generous = Make<DifficultyDefinition>(so => so.FindProperty("lootMultiplier").floatValue = 1.5f);
        Assert.AreEqual(2, DifficultyRules.LootCount(1, generous));
        Assert.AreEqual(5, DifficultyRules.LootCount(3, generous));
    }

    [Test]
    public void RainBarrel_FillsByRainTime_StopsAtCapacity()
    {
        (int stored, float partial) = RainBarrelRules.Fill(0, 0f, 3, 1.2f, 30);
        Assert.AreEqual(0, stored);
        Assert.AreEqual(0.6f, partial, 0.0001f);

        (stored, partial) = RainBarrelRules.Fill(stored, partial, 3, 1.2f, 30);
        Assert.AreEqual(1, stored);
        Assert.AreEqual(0.2f, partial, 0.0001f);

        (stored, partial) = RainBarrelRules.Fill(stored, partial, 3, 1.2f, 600);
        Assert.AreEqual(3, stored);
        Assert.AreEqual(0f, partial);
    }
}
