using Game.Combat;
using NUnit.Framework;

public sealed class HealthTests
{
    [Test]
    public void Damage_ReducesCurrentByAmount()
    {
        var health = new Health(50f);

        health.Damage(20f);

        Assert.AreEqual(30f, health.Current);
    }

    [Test]
    public void Damage_ClampsAtZero_DoesNotGoNegative()
    {
        var health = new Health(10f);

        health.Damage(999f);

        Assert.AreEqual(0f, health.Current);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void Damage_FiresDiedOnlyOnceWhenCrossingZero()
    {
        var health = new Health(10f);
        int diedCount = 0;
        health.Died += () => diedCount++;

        health.Damage(10f);
        health.Damage(5f); // 이미 죽은 뒤에는 Damage가 아무 일도 하지 않아야 한다

        Assert.AreEqual(1, diedCount);
    }

    [Test]
    public void Damage_ReportsActualAmountDealt_NotRequestedAmount()
    {
        var health = new Health(5f);
        float reported = -1f;
        health.Damaged += amount => reported = amount;

        health.Damage(999f);

        Assert.AreEqual(5f, reported);
    }

    [Test]
    public void Damage_ZeroOrNegativeAmount_DoesNothing()
    {
        var health = new Health(10f);

        health.Damage(0f);
        health.Damage(-5f);

        Assert.AreEqual(10f, health.Current);
    }
}
