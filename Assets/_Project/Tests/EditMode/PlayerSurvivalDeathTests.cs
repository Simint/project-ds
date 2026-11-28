using Game.Survival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CheckDeath()는 ApplyHealthRule(분이 지날 때마다)뿐 아니라 FallDamage처럼 HP를 바로 깎는 곳에서도
/// 불려야 한다 — 다음 분까지 기다리지 않고 그 자리에서 사망 판정하는 것이 이번에 고친 버그다.
/// PlayerSurvival.Stats는 Awake 없이도(database만 있으면) 지연 생성되므로 Awake를 거치지 않는다.
/// </summary>
public sealed class PlayerSurvivalDeathTests
{
    private GameObject go;
    private PlayerSurvival survival;
    private SurvivalStatDatabase database;
    private SurvivalStatDefinition hp;
    private SurvivalSettings settings;

    [SetUp]
    public void SetUp()
    {
        hp = ScriptableObject.CreateInstance<SurvivalStatDefinition>();
        SetField(hp, "id", "hp");
        SetField(hp, "min", 0f);
        SetField(hp, "max", 100f);
        SetField(hp, "startValue", 50f);
        SetField(hp, "changePerGameHour", 0f);

        database = ScriptableObject.CreateInstance<SurvivalStatDatabase>();
        var dbSo = new SerializedObject(database);
        SerializedProperty stats = dbSo.FindProperty("stats");
        stats.arraySize = 1;
        stats.GetArrayElementAtIndex(0).objectReferenceValue = hp;
        dbSo.ApplyModifiedPropertiesWithoutUndo();

        go = new GameObject("Player");
        survival = go.AddComponent<PlayerSurvival>();
        var so = new SerializedObject(survival);
        so.FindProperty("database").objectReferenceValue = database;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(database);
        Object.DestroyImmediate(hp);
        if (settings != null) Object.DestroyImmediate(settings);
    }

    private static void SetField(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [Test]
    public void CheckDeath_FiresImmediately_WhenHpDropsToZeroOutsideTheMinuteTick()
    {
        bool died = false;
        survival.Died += () => died = true;

        // FallDamage가 하는 것과 똑같이: 분 틱을 거치지 않고 바로 HP를 0으로 깎은 뒤 CheckDeath를 부른다.
        survival.Stats.Get(StatIds.Hp).Add(-999f);
        survival.CheckDeath();

        Assert.IsTrue(died);
    }

    [Test]
    public void CheckDeath_DoesNotFireTwice_WhileStillAtZero()
    {
        int diedCount = 0;
        survival.Died += () => diedCount++;
        survival.Stats.Get(StatIds.Hp).Add(-999f);

        survival.CheckDeath();
        survival.CheckDeath();

        Assert.AreEqual(1, diedCount);
    }

    [Test]
    public void CheckDeath_FiresAgain_AfterReviveAndDyingAgain()
    {
        int diedCount = 0;
        survival.Died += () => diedCount++;
        survival.Stats.Get(StatIds.Hp).Add(-999f);
        survival.CheckDeath();

        survival.Revive();
        survival.Stats.Get(StatIds.Hp).Add(-999f);
        survival.CheckDeath();

        Assert.AreEqual(2, diedCount);
    }

    [Test]
    public void AdvancingManyHours_StopsAfterFirstDeath_SoRevivedBodyIsNotKilledAgain()
    {
        // 시간당 HP가 줄어드는 상태에서 몇 시간을 한 번에 건너뛰면, 도중에 한 번 죽고(PlayerDeath처럼 바로 부활)
        // 남은 시간은 계산하지 않아야 한다 — 그렇지 않으면 부활한 몸이 또 죽어 사망 주머니가 덮어써진다.
        SetField(hp, "changePerGameHour", -30f);
        settings = ScriptableObject.CreateInstance<SurvivalSettings>();
        var so = new SerializedObject(survival);
        so.FindProperty("settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();
        int diedCount = 0;
        survival.Died += () => { diedCount++; survival.Revive(); };

        System.Reflection.MethodInfo handleAdvanced = typeof(PlayerSurvival)
            .GetMethod("HandleAdvanced", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(handleAdvanced, "PlayerSurvival.HandleAdvanced 이름이 바뀌었으면 이 테스트도 고친다.");
        handleAdvanced.Invoke(survival, new object[] { 10L * 60 });

        Assert.AreEqual(1, diedCount);
        Assert.Greater(survival.Stats.Get(StatIds.Hp).Value, 0f);
    }
}
