using Game.Survival;
using NUnit.Framework;

public sealed class StatusEffectsTests
{
    [Test]
    public void Apply_SetsActiveWithNoDuration_AsUntilCured()
    {
        var effects = new StatusEffects();

        effects.Apply("fracture", nowMinute: 100, durationMinutes: 0);

        Assert.IsTrue(effects.Has("fracture"));
    }

    [Test]
    public void Apply_Twice_DoesNotExtendOrDuplicate()
    {
        var effects = new StatusEffects();
        effects.Apply("sickness", nowMinute: 0, durationMinutes: 60);

        effects.Apply("sickness", nowMinute: 50, durationMinutes: 60);

        int count = 0;
        foreach (string _ in effects.ActiveIds) count++;
        Assert.AreEqual(1, count);
    }

    [Test]
    public void Cure_RemovesEffectAndFiresEvent()
    {
        var effects = new StatusEffects();
        effects.Apply("fracture", 0, 0);
        string cleared = null;
        effects.EffectCleared += id => cleared = id;

        effects.Cure("fracture");

        Assert.IsFalse(effects.Has("fracture"));
        Assert.AreEqual("fracture", cleared);
    }

    [Test]
    public void ExpireDue_ClearsOnlyEffectsPastTheirUntilMinute()
    {
        var effects = new StatusEffects();
        effects.Apply("sickness", nowMinute: 0, durationMinutes: 60);
        effects.Apply("fracture", nowMinute: 0, durationMinutes: 0);

        effects.ExpireDue(nowMinute: 61);

        Assert.IsFalse(effects.Has("sickness"));
        Assert.IsTrue(effects.Has("fracture"));
    }

    [Test]
    public void SetFromSave_DoesNotFireEvents()
    {
        var effects = new StatusEffects();
        bool fired = false;
        effects.EffectApplied += _ => fired = true;

        effects.SetFromSave(new (string id, long untilMinute)[] { ("fracture", 0) });

        Assert.IsTrue(effects.Has("fracture"));
        Assert.IsFalse(fired);
    }

    [Test]
    public void SetFromSave_FiresEffectCleared_ForEffectsThatDropOut()
    {
        // 지금 걸려 있던 효과가 새로 불러온 목록에는 없으면, 그 효과가 걸어 둔 이동 배율·스탯 모디파이어가
        // 남지 않도록 EffectCleared를 쏴야 한다(그냥 Dictionary.Clear만 하면 이벤트 없이 사라진다).
        var effects = new StatusEffects();
        effects.Apply("fracture", 0, 0);
        string cleared = null;
        effects.EffectCleared += id => cleared = id;

        effects.SetFromSave(new (string id, long untilMinute)[] { ("sickness", 0) });

        Assert.AreEqual("fracture", cleared);
        Assert.IsFalse(effects.Has("fracture"));
        Assert.IsTrue(effects.Has("sickness"));
    }

    [Test]
    public void SetFromSave_EffectKeptInBothLists_DoesNotFireCleared()
    {
        var effects = new StatusEffects();
        effects.Apply("fracture", 0, 0);
        bool clearedFired = false;
        effects.EffectCleared += _ => clearedFired = true;

        effects.SetFromSave(new (string id, long untilMinute)[] { ("fracture", 123) });

        Assert.IsFalse(clearedFired);
        Assert.IsTrue(effects.Has("fracture"));
    }
}
