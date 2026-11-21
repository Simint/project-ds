using Game.Survival;
using NUnit.Framework;
using UnityEngine;

public sealed class SurvivalStatsTests
{
    private static SurvivalStatDefinition MakeDefinition(string id, float min, float max, float start, float changePerHour)
    {
        var def = ScriptableObject.CreateInstance<SurvivalStatDefinition>();
        SerializedObjectSet(def, "id", id);
        SerializedObjectSet(def, "min", min);
        SerializedObjectSet(def, "max", max);
        SerializedObjectSet(def, "startValue", start);
        SerializedObjectSet(def, "changePerGameHour", changePerHour);
        return def;
    }

    private static void SerializedObjectSet(Object target, string field, object value)
    {
        var so = new UnityEditor.SerializedObject(target);
        var prop = so.FindProperty(field);
        switch (value)
        {
            case string s: prop.stringValue = s; break;
            case float f: prop.floatValue = f; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SurvivalStatDatabase MakeDatabase(params SurvivalStatDefinition[] defs)
    {
        var db = ScriptableObject.CreateInstance<SurvivalStatDatabase>();
        var so = new UnityEditor.SerializedObject(db);
        var prop = so.FindProperty("stats");
        prop.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return db;
    }

    [Test]
    public void Tick_AppliesBaseRatePerHour()
    {
        SurvivalStatDefinition hunger = MakeDefinition("hunger", 0f, 100f, 100f, -10f);
        var stats = new SurvivalStats(MakeDatabase(hunger));

        stats.Tick(1f);

        Assert.AreEqual(90f, stats.Get("hunger").Value, 0.001f);
    }

    [Test]
    public void Tick_ClampsAtMin()
    {
        SurvivalStatDefinition hunger = MakeDefinition("hunger", 0f, 100f, 5f, -10f);
        var stats = new SurvivalStats(MakeDatabase(hunger));

        stats.Tick(1f);

        Assert.AreEqual(0f, stats.Get("hunger").Value, 0.001f);
        Assert.IsTrue(stats.Get("hunger").IsAtMin);
    }

    [Test]
    public void SetModifier_MultiplierScalesRate()
    {
        SurvivalStatDefinition hunger = MakeDefinition("hunger", 0f, 100f, 100f, -10f);
        var stats = new SurvivalStats(MakeDatabase(hunger));

        stats.SetModifier("beast", "hunger", StatModifier.Multiply(2f));
        stats.Tick(1f);

        Assert.AreEqual(80f, stats.Get("hunger").Value, 0.001f);
    }

    [Test]
    public void ClearModifier_RestoresBaseRate()
    {
        SurvivalStatDefinition hunger = MakeDefinition("hunger", 0f, 100f, 100f, -10f);
        var stats = new SurvivalStats(MakeDatabase(hunger));
        stats.SetModifier("beast", "hunger", StatModifier.Multiply(2f));

        stats.ClearModifier("beast", "hunger");
        stats.Tick(1f);

        Assert.AreEqual(90f, stats.Get("hunger").Value, 0.001f);
    }

    [Test]
    public void SetModifier_AdditiveAddsOnTopOfBaseRate()
    {
        SurvivalStatDefinition temperature = MakeDefinition("temperature", 0f, 100f, 100f, 0f);
        var stats = new SurvivalStats(MakeDatabase(temperature));

        stats.SetModifier("environment", "temperature", StatModifier.Additive(-8f));
        stats.Tick(2f);

        Assert.AreEqual(84f, stats.Get("temperature").Value, 0.001f);
    }

    [Test]
    public void RestoreValues_SetsAndClamps()
    {
        SurvivalStatDefinition hp = MakeDefinition("hp", 0f, 100f, 100f, 0f);
        var stats = new SurvivalStats(MakeDatabase(hp));

        stats.RestoreValues(new (string id, float value)[] { ("hp", 500f) });

        Assert.AreEqual(100f, stats.Get("hp").Value, 0.001f);
    }
}
