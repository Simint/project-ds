using Game.Building;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HubBuildAreaTests
{
    private GameObject go;
    private HubBuildArea area;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("HubBuildArea");
        area = go.AddComponent<HubBuildArea>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    private static void SetBaseAreas(HubBuildArea target, params RectInt[] rects)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty("baseAreas");
        prop.arraySize = rects.Length;
        for (int i = 0; i < rects.Length; i++)
            prop.GetArrayElementAtIndex(i).rectIntValue = rects[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [Test]
    public void IsCellAllowed_TrueInsideBaseArea()
    {
        SetBaseAreas(area, new RectInt(0, 0, 10, 5));

        Assert.IsTrue(area.IsCellAllowed(new Vector2Int(3, 2)));
    }

    [Test]
    public void IsCellAllowed_FalseOutsideAnyArea()
    {
        SetBaseAreas(area, new RectInt(0, 0, 10, 5));

        Assert.IsFalse(area.IsCellAllowed(new Vector2Int(20, 20)));
        Assert.IsFalse(area.IsCellAllowed(new Vector2Int(-1, 0)));
    }

    [Test]
    public void UnlockArea_AllowsCellsInsideTheNewlyUnlockedRect()
    {
        SetBaseAreas(area, new RectInt(0, 0, 10, 5));
        var cellBeforeUnlock = new Vector2Int(15, 2);
        Assert.IsFalse(area.IsCellAllowed(cellBeforeUnlock));

        area.UnlockArea(new RectInt(10, 0, 10, 5));

        Assert.IsTrue(area.IsCellAllowed(cellBeforeUnlock));
    }

    [Test]
    public void UnlockArea_CalledTwiceWithSameRect_DoesNotDuplicate()
    {
        var rect = new RectInt(10, 0, 5, 5);
        area.UnlockArea(rect);
        area.UnlockArea(rect);

        Assert.IsTrue(area.IsCellAllowed(new Vector2Int(12, 2)));
    }
}
