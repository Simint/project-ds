using System.Reflection;
using Game.Player;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// ApplyKnockback이 실제로 KnockbackMotionState로 넘어가는지가 핵심이다 — Grounded는 매 FixedUpdate
/// 수평 속도를 입력대로 덮어써서, 그냥 velocity만 바꿔서는 다음 틱에 바로 지워진다
/// (PlayerControllerTeleportTests와 같은 이유로 body 필드만 리플렉션으로 채운다).
/// </summary>
public sealed class PlayerControllerKnockbackTests
{
    private GameObject go;
    private PlayerController controller;
    private Rigidbody2D body;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("Player");
        controller = go.AddComponent<PlayerController>();
        body = go.GetComponent<Rigidbody2D>();
        typeof(PlayerController).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, body);
        SetPrivate("currentState", GetPrivate("groundedState"));
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    private void SetPrivate(string field, object value) =>
        typeof(PlayerController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, value);

    private object GetPrivate(string field) =>
        typeof(PlayerController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);

    [Test]
    public void ApplyKnockback_MovesToKnockbackState_AndAppliesVelocityImmediately()
    {
        controller.ApplyKnockback(new Vector2(-5f, 2f), 0.3f);

        Assert.AreSame(GetPrivate("knockbackState"), GetPrivate("currentState"));
        Assert.AreEqual(new Vector2(-5f, 2f), body.linearVelocity);
    }

    [Test]
    public void ApplyKnockback_Twice_OverridesPreviousKnockbackVelocity()
    {
        controller.ApplyKnockback(new Vector2(-5f, 0f), 0.3f);

        controller.ApplyKnockback(new Vector2(8f, 1f), 0.1f);

        Assert.AreEqual(new Vector2(8f, 1f), body.linearVelocity);
    }

    // 맞기 직전에 점프를 눌렀거나(버퍼) 땅에서 코요테 타임이 남아 있어도, 다음 FixedUpdate에서
    // 넉백이 점프로 취소되면 안 된다 (unity-reviewer 지적).
    [Test]
    public void FixedUpdate_DuringKnockback_BufferedJumpDoesNotCancelKnockback()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        try
        {
            SetPrivate("settings", settings);
            SetPrivate("jumpBufferTimer", 1f);
            SetPrivate("coyoteTimer", 1f);

            controller.ApplyKnockback(new Vector2(-5f, 0f), 1f);
            InvokeFixedUpdate();

            Assert.AreSame(GetPrivate("knockbackState"), GetPrivate("currentState"));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    // 넉백 중에 새로 누른 점프도 넉백을 끊지 않는다.
    [Test]
    public void FixedUpdate_JumpPressedDuringKnockback_DoesNotCancelKnockback()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        try
        {
            SetPrivate("settings", settings);
            controller.ApplyKnockback(new Vector2(-5f, 0f), 1f);
            SetPrivate("jumpBufferTimer", 1f);
            SetPrivate("coyoteTimer", 1f);

            InvokeFixedUpdate();

            Assert.AreSame(GetPrivate("knockbackState"), GetPrivate("currentState"));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    private void InvokeFixedUpdate() =>
        typeof(PlayerController).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);
}
