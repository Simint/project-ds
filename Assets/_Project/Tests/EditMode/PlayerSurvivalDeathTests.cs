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
}
