using Game.Survival;
using NUnit.Framework;

public sealed class SleepRulesTests
{
    [Test]
    public void Healthy_CanSleep()
    {
        Assert.AreEqual(SleepBlockReason.None, SleepRules.Evaluate(false, false, false));
    }

    [Test]
    public void Starving_CannotSleep()
    {
        Assert.AreEqual(SleepBlockReason.Starving, SleepRules.Evaluate(true, false, false));
    }

    [Test]
    public void Bleeding_CannotSleep()
    {
        Assert.AreEqual(SleepBlockReason.Bleeding, SleepRules.Evaluate(false, true, false));
    }

    [Test]
    public void UntreatedFracture_CannotSleep()
    {
        Assert.AreEqual(SleepBlockReason.Fracture, SleepRules.Evaluate(false, false, true));
    }

    [Test]
    public void SeveralReasons_ReportsBleedingFirst()
    {
        Assert.AreEqual(SleepBlockReason.Bleeding, SleepRules.Evaluate(true, true, true));
        Assert.AreEqual(SleepBlockReason.Fracture, SleepRules.Evaluate(true, false, true));
    }
}
