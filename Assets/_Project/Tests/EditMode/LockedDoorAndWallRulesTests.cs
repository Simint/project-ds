using Game.Combat;
using Game.Items;
using Game.Maps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 잠긴 문 판정(열쇠가 먼저, 사람 손만, 닳은 쇠지렛대는 못 씀)과 벽 내구 계산(발톱은 배율)을 확인한다
/// (docs/design/GAME_DESIGN.md 4.3의 7단계 범위). 수치는 임의 값이다.
/// </summary>
public sealed class LockedDoorAndWallRulesTests
{
    private ItemDefinition crowbar;
    private ItemDefinition axe;

    [SetUp]
    public void SetUp()
    {
        crowbar = MakeTool(ToolKind.Crowbar);
        axe = MakeTool(ToolKind.Axe);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(crowbar);
        Object.DestroyImmediate(axe);
    }

    private static ItemDefinition MakeTool(ToolKind kind)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        var so = new SerializedObject(item);
        so.FindProperty("isTool").boolValue = true;
        so.FindProperty("tool").FindPropertyRelative("kind").enumValueIndex = (int)kind;
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    [Test]
    public void Resolve_BeastHand_NeverOpens_EvenWithKey()
    {
        Assert.AreEqual(UnlockMethod.None, LockedDoorRules.Resolve(false, true, crowbar, 5));
    }

    [Test]
    public void Resolve_HumanWithKey_PrefersKeyOverCrowbar()
    {
        Assert.AreEqual(UnlockMethod.Key, LockedDoorRules.Resolve(true, true, crowbar, 5));
    }

    [Test]
    public void Resolve_HumanWithoutKey_HoldingCrowbar_UsesCrowbar()
    {
        Assert.AreEqual(UnlockMethod.Crowbar, LockedDoorRules.Resolve(true, false, crowbar, 5));
    }

    [Test]
    public void Resolve_WornOutCrowbar_CannotOpen()
    {
        Assert.AreEqual(UnlockMethod.None, LockedDoorRules.Resolve(true, false, crowbar, 0));
    }

    [Test]
    public void Resolve_OtherTool_CannotOpen()
    {
        Assert.AreEqual(UnlockMethod.None, LockedDoorRules.Resolve(true, false, axe, 5));
    }

    [Test]
    public void DamageToWall_Claw_IsScaledByMultiplier()
    {
        var claw = new DamageInfo(7f, DamageKind.Claw, 0f, Vector2.zero, null);
        Assert.AreEqual(2.1f, BreakableWallRules.DamageToWall(claw, 0.3f), 0.0001f);
    }

    [Test]
    public void DamageToWall_Blunt_IsUnscaled()
    {
        var blunt = new DamageInfo(7f, DamageKind.Blunt, 0f, Vector2.zero, null);
        Assert.AreEqual(7f, BreakableWallRules.DamageToWall(blunt, 0.3f), 0.0001f);
    }

    [Test]
    public void DamageToWall_Tool_IsUnscaled()
    {
        var tool = new DamageInfo(5f, DamageKind.Tool, 0f, Vector2.zero, null);
        Assert.AreEqual(5f, BreakableWallRules.DamageToWall(tool, 0.3f), 0.0001f);
    }

    [Test]
    public void DamageToWall_Blade_DoesNothing()
    {
        var blade = new DamageInfo(7f, DamageKind.Blade, 0f, Vector2.zero, null);
        Assert.IsFalse(BreakableWallRules.CanDamageWall(DamageKind.Blade));
        Assert.AreEqual(0f, BreakableWallRules.DamageToWall(blade, 0.3f));
    }

    [Test]
    public void ClawSelfDamage_StopsAtOneHp()
    {
        Assert.AreEqual(2f, BreakableWallRules.ClawSelfDamage(10f, 2f), 0.0001f);
        Assert.AreEqual(0.5f, BreakableWallRules.ClawSelfDamage(1.5f, 2f), 0.0001f);
        Assert.AreEqual(0f, BreakableWallRules.ClawSelfDamage(1f, 2f), 0.0001f);
        Assert.AreEqual(0f, BreakableWallRules.ClawSelfDamage(0.5f, 2f), 0.0001f);
    }
}
