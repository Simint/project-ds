using Game.Core;
using Game.Maps;
using NUnit.Framework;

public sealed class MapStateRegistryTests
{
    private sealed class FakePersistentObject : IPersistentObject
    {
        public string Id { get; }
        public int State;
        public long UntilMinute;

        public FakePersistentObject(string id, int state = 0, long untilMinute = 0)
        {
            Id = id;
            State = state;
            UntilMinute = untilMinute;
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(State, UntilMinute, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            State = record.State;
            UntilMinute = record.UntilMinute;
        }
    }

    [Test]
    public void Restore_DoesNothing_ForMapNeverVisited()
    {
        var registry = new MapStateRegistry();
        var obj = new FakePersistentObject("a", state: 0);

        registry.Restore("map_never_visited", new[] { obj }, nowMinute: 100);

        Assert.AreEqual(0, obj.State);
    }

    [Test]
    public void CaptureThenRestore_RoundTripsState()
    {
        var registry = new MapStateRegistry();
        var obj = new FakePersistentObject("tree_1", state: 1, untilMinute: 500);

        registry.Capture("map_a", new[] { obj }, nowMinute: 100);

        var freshObj = new FakePersistentObject("tree_1");
        registry.Restore("map_a", new[] { freshObj }, nowMinute: 200);

        Assert.AreEqual(1, freshObj.State);
        Assert.AreEqual(500, freshObj.UntilMinute);
        Assert.AreEqual(100, registry.GetLastVisitedMinute("map_a"));
    }

    [Test]
    public void Restore_OnlyMatchesById()
    {
        var registry = new MapStateRegistry();
        registry.Capture("map_a", new[] { new FakePersistentObject("tree_1", state: 1) }, nowMinute: 0);

        var other = new FakePersistentObject("bush_2", state: 0);
        registry.Restore("map_a", new[] { other }, nowMinute: 10);

        Assert.AreEqual(0, other.State);
    }

    [Test]
    public void SetMapData_ThenRestore_AppliesLoadedState()
    {
        var registry = new MapStateRegistry();
        var loaded = new System.Collections.Generic.Dictionary<string, ObjectStateRecord>
        {
            ["cabinet_1"] = new ObjectStateRecord(1, 0, null)
        };

        registry.SetMapData("map_a", lastVisitedMinute: 42, loaded);

        var obj = new FakePersistentObject("cabinet_1");
        registry.Restore("map_a", new[] { obj }, nowMinute: 50);

        Assert.AreEqual(1, obj.State);
        Assert.AreEqual(42, registry.GetLastVisitedMinute("map_a"));
    }
}
