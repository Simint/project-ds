using System.Reflection;
using Game.Combat;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// body 필드만 리플렉션으로 채운다(PlayerControllerTeleportTests와 같은 이유 — Awake가 요구하는 다른
/// 참조는 이 테스트가 보려는 TakeDamage 동작과 무관하다).
/// </summary>
public sealed class EnemyHealthTests
{
    private GameObject go;
    private EnemyHealth health;
    private Rigidbody2D body;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("zombie");
        body = go.AddComponent<Rigidbody2D>();
        health = go.AddComponent<EnemyHealth>();
        typeof(EnemyHealth).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(health, body);
        health.Initialize(20f);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void TakeDamage_ReducesHealth_AndRaisesDamagedWithCurrentAndMax()
    {
        float reportedCurrent = -1f, reportedMax = -1f;
        health.Damaged += (current, max) => { reportedCurrent = current; reportedMax = max; };

        health.TakeDamage(new DamageInfo(8f, DamageKind.Blunt, 0f, Vector2.zero, null));

        Assert.AreEqual(12f, reportedCurrent);
        Assert.AreEqual(20f, reportedMax);
    }

    [Test]
    public void TakeDamage_LethalAmount_RaisesDied()
    {
        bool died = false;
        health.Died += _ => died = true;

        health.TakeDamage(new DamageInfo(999f, DamageKind.Claw, 0f, Vector2.zero, null));

        Assert.IsTrue(died);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void TakeDamage_WithKnockback_SetsBodyVelocity_AndMarksKnockedBack()
    {
        health.TakeDamage(new DamageInfo(1f, DamageKind.Blunt, 0f, new Vector2(3f, 0f), null));

        Assert.AreEqual(new Vector2(3f, 0f), body.linearVelocity);
        Assert.IsTrue(health.IsKnockedBack);
    }

    [Test]
    public void TakeDamage_NoKnockback_DoesNotTouchVelocity()
    {
        body.linearVelocity = new Vector2(1f, 1f);

        health.TakeDamage(new DamageInfo(1f, DamageKind.Blunt, 0f, Vector2.zero, null));

        Assert.AreEqual(new Vector2(1f, 1f), body.linearVelocity);
        Assert.IsFalse(health.IsKnockedBack);
    }

    [Test]
    public void TakeDamage_AfterDeath_DoesNothing()
    {
        health.TakeDamage(new DamageInfo(999f, DamageKind.Blunt, 0f, Vector2.zero, null));
        int damagedCount = 0;
        health.Damaged += (_, __) => damagedCount++;

        health.TakeDamage(new DamageInfo(5f, DamageKind.Blunt, 0f, Vector2.zero, null));

        Assert.AreEqual(0, damagedCount);
    }
}
