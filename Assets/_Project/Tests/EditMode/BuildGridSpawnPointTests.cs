using System.Reflection;
using Game.Building;
using Game.Core;
using Game.Maps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스폰 지점이 있는 셀은 건설 금지(끼임 방지). BuildGrid.Awake가 같은 씬의 SpawnPoint를 한 번만
/// 훑어 캐시하므로, 여기서도 Awake를 리플렉션으로 직접 불러 그 캐시를 채운다(DoorTests와 같은 이유 —
/// Edit Mode는 Awake를 자동으로 부르지 않는다).
/// </summary>
public sealed class BuildGridSpawnPointTests
{
    private GameObject gridGo;
    private GameObject spawnGo;
    private GameObject groundGo;
    private GameObject dummyPrefab;
    private GameObject structuresParentGo;
    private BuildGrid buildGrid;
    private StructureDefinition definition;
    private StructureDatabase database;
    private HubBuildArea buildArea;

    [SetUp]
    public void SetUp()
    {
        spawnGo = new GameObject("Spawn");
        spawnGo.AddComponent<SpawnPoint>();
        spawnGo.transform.position = new Vector3(0.5f, 0.5f, 0f); // 셀 (0,0) 안

        // 셀 (0,0)과 (5,0) 둘 다 Ground로 지지해 둔다 — (0,0)은 스폰 지점이라 막혀야 하고,
        // (5,0)은 비교용으로 똑같이 지지만 있고 스폰 지점은 없어 설치가 돼야 한다.
        groundGo = new GameObject("Ground");
        groundGo.layer = GameLayers.Ground;
        var groundCollider = groundGo.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(12f, 0.2f);
        groundGo.transform.position = new Vector3(3f, -0.1f, 0f);

        definition = ScriptableObject.CreateInstance<StructureDefinition>();
        dummyPrefab = new GameObject("DummyPrefab");
        var defSo = new SerializedObject(definition);
        defSo.FindProperty("prefab").objectReferenceValue = dummyPrefab;
        defSo.FindProperty("footprint").vector2IntValue = Vector2Int.one;
        defSo.FindProperty("kind").enumValueIndex = (int)StructureKind.Solid;
        defSo.ApplyModifiedPropertiesWithoutUndo();

        gridGo = new GameObject("BuildGrid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var buildAreaGo = new GameObject("HubBuildArea");
        buildArea = buildAreaGo.AddComponent<HubBuildArea>();
        var areaSo = new SerializedObject(buildArea);
        SerializedProperty areas = areaSo.FindProperty("baseAreas");
        areas.arraySize = 1;
        areas.GetArrayElementAtIndex(0).rectIntValue = new RectInt(-10, -10, 20, 20);
        areaSo.ApplyModifiedPropertiesWithoutUndo();

        structuresParentGo = new GameObject("Structures");
        database = ScriptableObject.CreateInstance<StructureDatabase>();

        buildGrid = gridGo.AddComponent<BuildGrid>();
        var gridSo = new SerializedObject(buildGrid);
        gridSo.FindProperty("grid").objectReferenceValue = grid;
        gridSo.FindProperty("buildArea").objectReferenceValue = buildArea;
        gridSo.FindProperty("structureDatabase").objectReferenceValue = database;
        gridSo.FindProperty("structuresParent").objectReferenceValue = structuresParentGo.transform;
        gridSo.ApplyModifiedPropertiesWithoutUndo();

        // 프로젝트의 Physics2DSettings는 AutoSyncTransforms가 꺼져 있어, transform.position을 직접
        // 바꾼 뒤 물리 쿼리(OverlapBox) 전에 명시적으로 동기화해야 한다(DoorTests와 같은 이유).
        Physics2D.SyncTransforms();
        InvokeAwake(buildGrid);
    }

    private static void InvokeAwake(Object target)
    {
        MethodInfo awake = target.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(awake);
        awake.Invoke(target, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gridGo);
        Object.DestroyImmediate(spawnGo);
        Object.DestroyImmediate(groundGo);
        Object.DestroyImmediate(dummyPrefab);
        Object.DestroyImmediate(structuresParentGo);
        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(database);
        Object.DestroyImmediate(buildArea.gameObject);
    }

    [Test]
    public void CanPlace_FalseOnSpawnPointCell()
    {
        Assert.IsFalse(buildGrid.CanPlace(definition, new Vector2Int(0, 0)));
    }

    [Test]
    public void CanPlace_TrueOnOrdinarySupportedCell()
    {
        Assert.IsTrue(buildGrid.CanPlace(definition, new Vector2Int(5, 0)));
    }
}
