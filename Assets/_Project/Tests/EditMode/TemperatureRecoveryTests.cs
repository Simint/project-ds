using Game.Survival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 체온이 실외 밤에는 내려가고, 그 외(실외 낮·실내·은신처)에는 서서히 회복되며 최대치에서 멈추는지
/// 확인한다 (PlayerSurvival.RefreshTemperatureModifier가 매 시간/맵 이동마다 다시 고르는 모디파이어).
/// </summary>
public sealed class TemperatureRecoveryTests
{
    private const float NightDropPerHour = -8f;
    private const float DayRecoveryPerHour = 4f;
    private const float IndoorRecoveryPerHour = 10f;
    private const int NightStartHour = 20;
    private const int NightEndHour = 6;

    private static SurvivalSettings MakeSettings()
    {
        var settings = ScriptableObject.CreateInstance<SurvivalSettings>();
        var so = new SerializedObject(settings);
        so.FindProperty("nightOutdoorTemperatureDropPerHour").floatValue = NightDropPerHour;
        so.FindProperty("temperatureRecoveryPerHourOutdoorDay").floatValue = DayRecoveryPerHour;
        so.FindProperty("temperatureRecoveryPerHourIndoor").floatValue = IndoorRecoveryPerHour;
        so.FindProperty("nightStartHour").intValue = NightStartHour;
        so.FindProperty("nightEndHour").intValue = NightEndHour;
        so.ApplyModifiedPropertiesWithoutUndo();
        return settings;
    }

    private static SurvivalStatDefinition MakeTemperatureDefinition()
    {
        var def = ScriptableObject.CreateInstance<SurvivalStatDefinition>();
        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue = "temperature";
        so.FindProperty("min").floatValue = 0f;
        so.FindProperty("max").floatValue = 100f;
        so.FindProperty("startValue").floatValue = 100f;
        so.FindProperty("changePerGameHour").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return def;
    }

    private static SurvivalStatDatabase MakeDatabase(SurvivalStatDefinition def)
    {
        var db = ScriptableObject.CreateInstance<SurvivalStatDatabase>();
        var so = new SerializedObject(db);
        SerializedProperty prop = so.FindProperty("stats");
        prop.arraySize = 1;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = def;
        so.ApplyModifiedPropertiesWithoutUndo();
        return db;
    }

    [Test]
    public void ComputeTemperatureModifier_NightOutdoor_Drops()
    {
        SurvivalSettings settings = MakeSettings();

        StatModifier modifier = settings.ComputeTemperatureModifier(isSafeFromWeather: false, hour: 23);

        Assert.AreEqual(NightDropPerHour, modifier.AdditivePerHour, 0.001f);
    }

    [Test]
    public void ComputeTemperatureModifier_DayOutdoor_Recovers()
    {
        SurvivalSettings settings = MakeSettings();

        StatModifier modifier = settings.ComputeTemperatureModifier(isSafeFromWeather: false, hour: 12);

        Assert.AreEqual(DayRecoveryPerHour, modifier.AdditivePerHour, 0.001f);
    }

    [Test]
    public void ComputeTemperatureModifier_IndoorAtNight_RecoversInstead()
    {
        SurvivalSettings settings = MakeSettings();

        StatModifier modifier = settings.ComputeTemperatureModifier(isSafeFromWeather: true, hour: 23);

        Assert.AreEqual(IndoorRecoveryPerHour, modifier.AdditivePerHour, 0.001f);
    }

    [Test]
    public void Tick_AtNightOutdoor_LowersTemperatureOverTime()
    {
        SurvivalStatDefinition temperature = MakeTemperatureDefinition();
        var stats = new SurvivalStats(MakeDatabase(temperature));
        SurvivalSettings settings = MakeSettings();

        stats.SetModifier("environment", "temperature", settings.ComputeTemperatureModifier(false, 23));
        stats.Tick(1f);

        Assert.AreEqual(92f, stats.Get("temperature").Value, 0.001f);
    }

    [Test]
    public void Tick_IndoorAfterNightDrop_RecoversAndEventuallyCapsAtMax()
    {
        SurvivalStatDefinition temperature = MakeTemperatureDefinition();
        var stats = new SurvivalStats(MakeDatabase(temperature));
        SurvivalSettings settings = MakeSettings();

        // 실외 밤에 세 시간 떨어뜨린 뒤 은신처로 들어간다.
        stats.SetModifier("environment", "temperature", settings.ComputeTemperatureModifier(false, 23));
        stats.Tick(3f);
        Assert.AreEqual(76f, stats.Get("temperature").Value, 0.001f);

        stats.SetModifier("environment", "temperature", settings.ComputeTemperatureModifier(true, 23));
        stats.Tick(2f);
        Assert.AreEqual(96f, stats.Get("temperature").Value, 0.001f);

        // 맵 이동으로 많은 시간을 한 번에 건너뛰어도(Advance) 같은 Tick이 최대치에서 멈춘다.
        stats.Tick(10f);
        Assert.AreEqual(100f, stats.Get("temperature").Value, 0.001f);
    }

    [Test]
    public void Tick_DayOutdoor_RecoversSlowlyWithoutExceedingMax()
    {
        SurvivalStatDefinition temperature = MakeTemperatureDefinition();
        var so = new SerializedObject(temperature);
        so.FindProperty("startValue").floatValue = 98f;
        so.ApplyModifiedPropertiesWithoutUndo();
        var stats = new SurvivalStats(MakeDatabase(temperature));
        SurvivalSettings settings = MakeSettings();

        stats.SetModifier("environment", "temperature", settings.ComputeTemperatureModifier(false, 12));
        stats.Tick(5f);

        Assert.AreEqual(100f, stats.Get("temperature").Value, 0.001f);
    }
}
