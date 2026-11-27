using Game.Combat;
using Game.Player;
using Game.Survival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerSurvivalDeathTests와 같은 방식으로 HP 스탯 하나만 있는 최소 DB를 만든다. 출혈은
/// StatusEffectDatabase에 "bleeding" 정의가 있어야 PlayerStatusEffects.Apply가 실제로 건다.
/// </summary>
public sealed class PlayerDamageReceiverTests
{
    private GameObject go;
    private PlayerDamageReceiver receiver;
    private PlayerSurvival survival;
    private PlayerStatusEffects statusEffects;
    private PlayerController controller;
    private SurvivalStatDatabase statDatabase;
    private SurvivalStatDefinition hpDef;
    private StatusEffectDatabase effectDatabase;
    private StatusEffectDefinition bleedingDef;

    [SetUp]
    public void SetUp()
    {
        hpDef = ScriptableObject.CreateInstance<SurvivalStatDefinition>();
        SetField(hpDef, "id", StatIds.Hp);
        SetField(hpDef, "min", 0f);
        SetField(hpDef, "max", 100f);
        SetField(hpDef, "startValue", 50f);
        SetField(hpDef, "changePerGameHour", 0f);

        statDatabase = ScriptableObject.CreateInstance<SurvivalStatDatabase>();
        var statDbSo = new SerializedObject(statDatabase);
        SerializedProperty stats = statDbSo.FindProperty("stats");
        stats.arraySize = 1;
        stats.GetArrayElementAtIndex(0).objectReferenceValue = hpDef;
        statDbSo.ApplyModifiedPropertiesWithoutUndo();

        bleedingDef = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        SetField(bleedingDef, "id", StatusEffectIds.Bleeding);

        effectDatabase = ScriptableObject.CreateInstance<StatusEffectDatabase>();
        var effectDbSo = new SerializedObject(effectDatabase);
        SerializedProperty effects = effectDbSo.FindProperty("effects");
        effects.arraySize = 1;
        effects.GetArrayElementAtIndex(0).objectReferenceValue = bleedingDef;
        effectDbSo.ApplyModifiedPropertiesWithoutUndo();

        go = new GameObject("Player");

        survival = go.AddComponent<PlayerSurvival>();
        SetObjRef(survival, "database", statDatabase);

        controller = go.AddComponent<PlayerController>();

        statusEffects = go.AddComponent<PlayerStatusEffects>();
        SetObjRef(statusEffects, "database", effectDatabase);
        SetObjRef(statusEffects, "controller", controller);

        receiver = go.AddComponent<PlayerDamageReceiver>();
        SetObjRef(receiver, "survival", survival);
        SetObjRef(receiver, "statusEffects", statusEffects);
        SetObjRef(receiver, "controller", controller);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(statDatabase);
        Object.DestroyImmediate(hpDef);
        Object.DestroyImmediate(effectDatabase);
        Object.DestroyImmediate(bleedingDef);
    }

    private static void SetField(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [Test]
    public void TakeDamage_ReducesPlayerHp()
    {
        receiver.TakeDamage(new DamageInfo(15f, DamageKind.Claw, 0f, Vector2.zero, null));

        Assert.AreEqual(35f, survival.Stats.Get(StatIds.Hp).Value);
    }

    [Test]
    public void TakeDamage_ZeroBleedChance_NeverAppliesBleeding()
    {
        receiver.TakeDamage(new DamageInfo(5f, DamageKind.Claw, 0f, Vector2.zero, null));

        Assert.IsFalse(statusEffects.Effects.Has(StatusEffectIds.Bleeding));
    }

    [Test]
    public void TakeDamage_FullBleedChance_AlwaysAppliesBleeding()
    {
        receiver.TakeDamage(new DamageInfo(5f, DamageKind.Claw, 1f, Vector2.zero, null));

        Assert.IsTrue(statusEffects.Effects.Has(StatusEffectIds.Bleeding));
    }

    [Test]
    public void TakeDamage_LethalAmount_FiresDied()
    {
        bool died = false;
        survival.Died += () => died = true;

        receiver.TakeDamage(new DamageInfo(999f, DamageKind.Claw, 0f, Vector2.zero, null));

        Assert.IsTrue(died);
    }
}
