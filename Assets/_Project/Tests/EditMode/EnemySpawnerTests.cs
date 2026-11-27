using Game.AI;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HarvestableRestoreTests와 같은 이유로 "리스폰 시각을 다시 계산해 두 배로 늘리지 않는지"만 본다 —
/// 실제 생성·제거(SpawnAll/HandleCreatureDied)는 Instantiate/Destroy를 쓰는 통합 동작이라 Play Mode
/// 스모크 테스트로 확인한다.
/// </summary>
public sealed class EnemySpawnerTests
{
    private GameObject root;
    private EnemySpawner spawner;
    private GameObject prefabGo;
    private CreatureDefinition definition;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<CreatureDefinition>();
        var defSo = new SerializedObject(definition);
        defSo.FindProperty("respawnDays").intValue = 1;
        defSo.ApplyModifiedPropertiesWithoutUndo();

        prefabGo = new GameObject("prefab_stand_in");
        prefabGo.AddComponent<Rigidbody2D>();
        var prefabBrain = prefabGo.AddComponent<Game.AI.CreatureBrain>();
        var brainSo = new SerializedObject(prefabBrain);
        brainSo.FindProperty("definition").objectReferenceValue = definition;
        brainSo.ApplyModifiedPropertiesWithoutUndo();

        root = new GameObject("spawner");
        root.AddComponent<PersistentId>();
        spawner = root.AddComponent<EnemySpawner>();
        var so = new SerializedObject(spawner);
        so.FindProperty("persistentId").objectReferenceValue = root.GetComponent<PersistentId>();
        so.FindProperty("prefab").objectReferenceValue = prefabBrain;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(prefabGo);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Restore_PendingRespawn_KeepsOriginalRespawnTime_InsteadOfDoublingIt()
    {
        spawner.Restore(new ObjectStateRecord(1, 5000, null), nowMinute: 1000);

        ObjectStateRecord captured = spawner.Capture();
        Assert.AreEqual(1, captured.State);
        Assert.AreEqual(5000, captured.UntilMinute);
    }

    [Test]
    public void Restore_RespawnTimeReached_RestoresAsNotDepleted()
    {
        spawner.Restore(new ObjectStateRecord(1, 500, null), nowMinute: 500);

        ObjectStateRecord captured = spawner.Capture();
        Assert.AreEqual(0, captured.State);
        Assert.AreEqual(0, captured.UntilMinute);
    }

    [Test]
    public void Restore_NotDepletedRecord_RestoresCleanly()
    {
        spawner.Restore(new ObjectStateRecord(0, 0, null), nowMinute: 1000);

        ObjectStateRecord captured = spawner.Capture();
        Assert.AreEqual(0, captured.State);
    }

    [Test]
    public void Capture_FreshSpawner_NeverDepleted()
    {
        ObjectStateRecord captured = spawner.Capture();

        Assert.AreEqual(0, captured.State);
    }
}
