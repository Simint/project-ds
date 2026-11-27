using Game.Combat;
using Game.Core;
using Game.Harvesting;
using Game.Maps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 잠긴 문·부술 수 있는 벽·땅 파기 지점의 저장 상태가 그대로 되돌아오는지(4.2 "영구히 남는 것"), 벽이 둔기와
/// 발톱에 다르게 깎이는지 확인한다. 수치는 임의 값이다.
/// </summary>
public sealed class ExplorationPersistenceTests
{
    private GameObject go;
    private BreakableWallDefinition wallDefinition;
    private ExplorationSettings settings;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("exploration");
        wallDefinition = ScriptableObject.CreateInstance<BreakableWallDefinition>();
        var defSo = new SerializedObject(wallDefinition);
        defSo.FindProperty("maxDurability").floatValue = 10f;
        defSo.ApplyModifiedPropertiesWithoutUndo();

        settings = ScriptableObject.CreateInstance<ExplorationSettings>();
        var setSo = new SerializedObject(settings);
        setSo.FindProperty("clawWallDamageMultiplier").floatValue = 0.1f;
        setSo.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(wallDefinition);
        Object.DestroyImmediate(settings);
    }

    private BreakableWall MakeWall()
    {
        var wall = go.AddComponent<BreakableWall>();
        var so = new SerializedObject(wall);
        so.FindProperty("definition").objectReferenceValue = wallDefinition;
        so.FindProperty("settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();
        wall.Restore(new ObjectStateRecord(0, 0, null), 0);
        return wall;
    }

    [Test]
    public void LockedDoor_RestoreOpenedByCrowbar_RoundTrips()
    {
        var door = go.AddComponent<LockedDoor>();
        door.Restore(new ObjectStateRecord(2, 0, null), 0);
        Assert.IsTrue(door.IsOpen);
        Assert.AreEqual(2, door.Capture().State);
    }

    [Test]
    public void LockedDoor_RestoreLocked_StaysLocked()
    {
        var door = go.AddComponent<LockedDoor>();
        door.Restore(new ObjectStateRecord(0, 0, null), 0);
        Assert.IsFalse(door.IsOpen);
    }

    [Test]
    public void DigSpot_RestoreDug_RoundTrips()
    {
        var spot = go.AddComponent<DigSpot>();
        spot.Restore(new ObjectStateRecord(1, 0, null), 0);
        Assert.IsTrue(spot.IsDug);
        Assert.AreEqual(1, spot.Capture().State);
    }

    [Test]
    public void BreakableWall_BluntHitsUntilZero_Breaks()
    {
        BreakableWall wall = MakeWall();
        var blunt = new DamageInfo(4f, DamageKind.Blunt, 0f, Vector2.zero, null);
        wall.TakeDamage(blunt);
        wall.TakeDamage(blunt);
        Assert.IsFalse(wall.IsBroken);
        wall.TakeDamage(blunt);
        Assert.IsTrue(wall.IsBroken);
        Assert.AreEqual(1, wall.Capture().State);
    }

    [Test]
    public void BreakableWall_ClawHit_TakesFarLessDurability()
    {
        BreakableWall wall = MakeWall();
        wall.TakeDamage(new DamageInfo(4f, DamageKind.Claw, 0f, Vector2.zero, null));
        Assert.AreEqual(9.6f, wall.RemainingDurability, 0.0001f);
        Assert.IsFalse(wall.IsBroken);
    }

    [Test]
    public void BreakableWall_RestoreBroken_StaysBroken_AndRestoreIntact_RefillsDurability()
    {
        BreakableWall wall = MakeWall();
        wall.TakeDamage(new DamageInfo(4f, DamageKind.Blunt, 0f, Vector2.zero, null));
        wall.Restore(new ObjectStateRecord(0, 0, null), 0);
        Assert.AreEqual(10f, wall.RemainingDurability, 0.0001f);

        wall.Restore(new ObjectStateRecord(1, 0, null), 0);
        Assert.IsTrue(wall.IsBroken);
    }
}
