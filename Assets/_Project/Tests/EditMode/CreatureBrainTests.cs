using System.Reflection;
using Game.AI;
using Game.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ChangeState/MoveToward/Stop는 internal이라(이 테스트 어셈블리에서는 안 보인다) 리플렉션으로
/// 호출한다 — PlayerControllerTeleportTests와 같은 이유. Start()는 부르지 않으므로(GameServices
/// 의존 없이 순수 상태 전환·이동 계산만 본다) currentState는 직접 리플렉션으로 맞춰 둔다.
/// </summary>
public sealed class CreatureBrainTests
{
    private GameObject go;
    private CreatureBrain brain;
    private Rigidbody2D body;
    private CreatureDefinition definition;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<CreatureDefinition>();
        var defSo = new SerializedObject(definition);
        defSo.FindProperty("moveSpeedTiles").floatValue = 4f;
        defSo.FindProperty("attackRangeTiles").floatValue = 1f;
        defSo.ApplyModifiedPropertiesWithoutUndo();

        go = new GameObject("creature");
        body = go.AddComponent<Rigidbody2D>();
        brain = go.AddComponent<CreatureBrain>();

        SetPrivate("definition", definition);
        SetPrivate("body", body);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(definition);
    }

    private void SetPrivate(string field, object value) =>
        typeof(CreatureBrain).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(brain, value);

    private object GetPrivate(string field) =>
        typeof(CreatureBrain).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(brain);

    private void InvokeInternal(string method, params object[] args) =>
        typeof(CreatureBrain).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(brain, args);

    [Test]
    public void MoveToward_TargetToTheRight_SetsPositiveHorizontalVelocity_PreservesVertical()
    {
        body.linearVelocity = new Vector2(0f, 5f); // 공중(중력으로 떨어지는 중)을 흉내

        InvokeInternal("MoveToward", new Vector2(10f, 0f));

        Assert.AreEqual(4f, body.linearVelocity.x);
        Assert.AreEqual(5f, body.linearVelocity.y);
    }

    [Test]
    public void MoveToward_TargetToTheLeft_SetsNegativeHorizontalVelocity()
    {
        body.position = new Vector2(10f, 0f);

        InvokeInternal("MoveToward", new Vector2(0f, 0f));

        Assert.AreEqual(-4f, body.linearVelocity.x);
    }

    [Test]
    public void MoveToward_WithinStopDistance_ZeroesHorizontalVelocity_PreservesVertical()
    {
        body.linearVelocity = new Vector2(3f, -2f);

        InvokeInternal("MoveToward", body.position); // 목표 = 바로 자기 자리

        Assert.AreEqual(0f, body.linearVelocity.x);
        Assert.AreEqual(-2f, body.linearVelocity.y);
    }

    [Test]
    public void Stop_ZeroesHorizontalVelocity_PreservesVertical()
    {
        body.linearVelocity = new Vector2(3f, -7f);

        InvokeInternal("Stop");

        Assert.AreEqual(Vector2.zero.x, body.linearVelocity.x);
        Assert.AreEqual(-7f, body.linearVelocity.y);
    }

    [Test]
    public void OnNoiseHeard_WhileIdleAndWithinRadius_TransitionsToInvestigate_AndRecordsTarget()
    {
        SetPrivate("currentState", GetPrivate("idleState"));

        brain.OnNoiseHeard(new Vector2(5f, 0f), 10f, NoiseKind.Footstep);

        Assert.AreSame(GetPrivate("investigateState"), GetPrivate("currentState"));
        Assert.AreEqual(new Vector2(5f, 0f), brain.InvestigateTarget);
    }

    [Test]
    public void OnNoiseHeard_OutsideRadius_StaysIdle()
    {
        SetPrivate("currentState", GetPrivate("idleState"));

        brain.OnNoiseHeard(new Vector2(50f, 0f), 10f, NoiseKind.Footstep);

        Assert.AreSame(GetPrivate("idleState"), GetPrivate("currentState"));
    }

    [Test]
    public void OnNoiseHeard_WhileAlreadyChasing_Ignored()
    {
        SetPrivate("currentState", GetPrivate("chaseState"));

        brain.OnNoiseHeard(new Vector2(1f, 0f), 10f, NoiseKind.Footstep);

        Assert.AreSame(GetPrivate("chaseState"), GetPrivate("currentState"));
    }
}
