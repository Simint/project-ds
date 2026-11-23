using System.Reflection;
using Game.Player;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// TeleportTo는 맵 이동 때 위치만 바꾸지 않는다 — Falling 상태가 이전 위치를 기준으로 낙하 거리를
/// 재는 것(FallingMotionState.startY)을 막기 위해 접지 상태로 되돌린다. Awake를 거치지 않고 body
/// 필드만 리플렉션으로 채운다(다른 참조가 비어 있다는 Debug.LogError를 피하기 위해 — 이 테스트는
/// TeleportTo 자체만 본다).
/// </summary>
public sealed class PlayerControllerTeleportTests
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
        body.linearVelocity = new Vector2(3f, -7f);

        SetPrivate("coyoteTimer", 0.2f);
        SetPrivate("jumpBufferTimer", 0.1f);
        SetPrivate("jumpCutRequested", true);
        // GroundedState/FallingState/ChangeState는 internal이라(이 테스트 어셈블리에서는 안 보인다)
        // 같은 이름의 private 필드를 리플렉션으로 직접 읽고 쓴다.
        SetPrivate("currentState", GetPrivate("fallingState"));
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    private void SetPrivate(string field, object value) =>
        typeof(PlayerController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, value);

    private object GetPrivate(string field) =>
        typeof(PlayerController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);

    [Test]
    public void TeleportTo_MovesBodyAndZeroesVelocity()
    {
        controller.TeleportTo(new Vector2(5f, 3f));

        Assert.AreEqual(new Vector2(5f, 3f), body.position);
        Assert.AreEqual(Vector2.zero, body.linearVelocity);
    }

    [Test]
    public void TeleportTo_ResetsJumpAndCoyoteTimers()
    {
        controller.TeleportTo(Vector2.zero);

        Assert.AreEqual(0f, (float)GetPrivate("coyoteTimer"));
        Assert.AreEqual(0f, (float)GetPrivate("jumpBufferTimer"));
        Assert.IsFalse((bool)GetPrivate("jumpCutRequested"));
    }

    [Test]
    public void TeleportTo_LeavesFallingState_SoNextLandingMeasuresFromTheNewPosition()
    {
        // Falling 상태 그대로였다면 FallingMotionState.startY가 텔레포트 전 위치로 남아, 도착 직후
        // 땅에 닿을 때 엉뚱하게 큰 낙하 거리로 계산된다.
        controller.TeleportTo(new Vector2(0f, 20f));

        Assert.AreSame(GetPrivate("groundedState"), GetPrivate("currentState"));
    }
}
