using Game.Lore;
using Game.Player;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 후각 대상 고르기(반경, 벽에 가로막힘, 버퍼 크기)와 쪽지 기록(중복 없음, 불러오기)을 확인한다
/// (docs/design/GAME_DESIGN.md 3.2, 4.3의 7단계 범위). 수치는 임의 값이다.
/// </summary>
public sealed class ScentAndLoreTests
{
    private static readonly Vector2[] Points = { new Vector2(1f, 0f), new Vector2(10f, 0f), new Vector2(-2f, 0f) };

    [Test]
    public void Select_OnlyWithinRadius()
    {
        int[] results = new int[4];
        int found = ScentQuery.Select(Vector2.zero, 3f, Points.Length, i => Points[i], null, results);
        Assert.AreEqual(2, found);
        Assert.AreEqual(0, results[0]);
        Assert.AreEqual(2, results[1]);
    }

    [Test]
    public void Select_SkipsBlockedTargets()
    {
        int[] results = new int[4];
        int found = ScentQuery.Select(Vector2.zero, 3f, Points.Length, i => Points[i], (from, to) => to.x < 0f, results);
        Assert.AreEqual(1, found);
        Assert.AreEqual(0, results[0]);
    }

    [Test]
    public void Select_StopsAtBufferSize()
    {
        int[] results = new int[1];
        int found = ScentQuery.Select(Vector2.zero, 100f, Points.Length, i => Points[i], null, results);
        Assert.AreEqual(1, found);
    }

    [Test]
    public void Select_ZeroRadius_FindsNothing()
    {
        int found = ScentQuery.Select(Vector2.zero, 0f, Points.Length, i => Points[i], null, new int[4]);
        Assert.AreEqual(0, found);
    }

    [Test]
    public void LoreCollection_Add_IgnoresDuplicatesAndEmpty()
    {
        var collection = new LoreCollection();
        Assert.IsTrue(collection.Add("note_a"));
        Assert.IsFalse(collection.Add("note_a"));
        Assert.IsFalse(collection.Add(""));
        Assert.AreEqual(1, collection.Ids.Count);
    }

    [Test]
    public void LoreCollection_KeepsPickupOrder()
    {
        var collection = new LoreCollection();
        collection.Add("note_b");
        collection.Add("note_a");
        CollectionAssert.AreEqual(new[] { "note_b", "note_a" }, collection.Ids);
    }

    [Test]
    public void LoreCollection_SetFromSave_DropsDuplicatesAndNull()
    {
        var collection = new LoreCollection();
        collection.Add("old");
        collection.SetFromSave(new[] { "x", null, "x", "y" });
        CollectionAssert.AreEqual(new[] { "x", "y" }, collection.Ids);
        Assert.IsFalse(collection.Contains("old"));
    }

    [Test]
    public void LoreCollection_SetFromSave_Null_ClearsInsteadOfThrowing()
    {
        var collection = new LoreCollection();
        collection.Add("a");
        collection.SetFromSave(null);
        Assert.AreEqual(0, collection.Ids.Count);
    }
}
