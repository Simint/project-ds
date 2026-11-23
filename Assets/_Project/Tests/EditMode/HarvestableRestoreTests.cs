using Game.Core;
using Game.Harvesting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HarvestableRestoreTests
{
    private GameObject root;
    private Harvestable harvestable;
    private HarvestableDefinition definition;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<HarvestableDefinition>();
        var defSo = new SerializedObject(definition);
        defSo.FindProperty("respawnDays").intValue = 1;
        defSo.ApplyModifiedPropertiesWithoutUndo();

        root = new GameObject("oak_tree");
        root.AddComponent<Game.Core.PersistentId>();
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(root.transform);
        var visual = visualGo.AddComponent<SpriteRenderer>();
        var interactCollider = root.AddComponent<BoxCollider2D>();

        harvestable = root.AddComponent<Harvestable>();
        var so = new SerializedObject(harvestable);
        so.FindProperty("definition").objectReferenceValue = definition;
        so.FindProperty("persistentId").objectReferenceValue = root.GetComponent<PersistentId>();
        so.FindProperty("visual").objectReferenceValue = visual;
        so.FindProperty("interactCollider").objectReferenceValue = interactCollider;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Restore_PendingRespawn_KeepsOriginalRespawnTime_InsteadOfDoublingIt()
    {
        // 리스폰 주기는 1일(1440분). 저장된 리스폰 시각(5000)이 아직 안 지났으면, 그 값을 그대로 써야
        // 한다 — SetDepleted가 "지금(1000) + 1440"으로 다시 계산하면 두 배로 늘어나는 버그였다.
        harvestable.Restore(new ObjectStateRecord(1, 5000, null), nowMinute: 1000);

        ObjectStateRecord captured = harvestable.Capture();
        Assert.AreEqual(1, captured.State);
        Assert.AreEqual(5000, captured.UntilMinute);
    }

    [Test]
    public void Restore_RespawnTimeReached_RestoresAsNotDepleted()
    {
        harvestable.Restore(new ObjectStateRecord(1, 500, null), nowMinute: 500);

        ObjectStateRecord captured = harvestable.Capture();
        Assert.AreEqual(0, captured.State);
        Assert.AreEqual(0, captured.UntilMinute);
    }

    [Test]
    public void Restore_NotDepletedRecord_RestoresCleanly()
    {
        harvestable.Restore(new ObjectStateRecord(0, 0, null), nowMinute: 1000);

        ObjectStateRecord captured = harvestable.Capture();
        Assert.AreEqual(0, captured.State);
    }
}
