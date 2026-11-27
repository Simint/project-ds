using Game.Save;
using NUnit.Framework;

public sealed class DeathBagRegistryTests
{
    [Test]
    public void FreshRegistry_HasNoBag()
    {
        var registry = new DeathBagRegistry();

        Assert.IsFalse(registry.HasBag);
    }

    [Test]
    public void Set_WithMapId_HasBagBecomesTrue()
    {
        var registry = new DeathBagRegistry();

        registry.Set(new DeathBagRecord { mapId = "map_residential", x = 3f, y = 1f });

        Assert.IsTrue(registry.HasBag);
        Assert.AreEqual("map_residential", registry.Current.mapId);
    }

    [Test]
    public void Set_Twice_OverwritesPreviousBag_InsteadOfKeepingBoth()
    {
        // "마지막 하나만 남는다. 회수하기 전에 또 죽으면 이전 주머니는 사라진다" (docs/design 5.8).
        var registry = new DeathBagRegistry();
        registry.Set(new DeathBagRecord { mapId = "map_residential", x = 1f, y = 1f });

        registry.Set(new DeathBagRecord { mapId = "map_hub", x = 2f, y = 2f });

        Assert.AreEqual("map_hub", registry.Current.mapId);
        Assert.AreEqual(2f, registry.Current.x);
    }

    [Test]
    public void Clear_RemovesTheBag()
    {
        var registry = new DeathBagRegistry();
        registry.Set(new DeathBagRecord { mapId = "map_residential" });

        registry.Clear();

        Assert.IsFalse(registry.HasBag);
    }

    [Test]
    public void SetFromSave_NullRecord_LeavesNoBag_InsteadOfThrowing()
    {
        var registry = new DeathBagRegistry();

        registry.SetFromSave(null);

        Assert.IsFalse(registry.HasBag);
    }

    [Test]
    public void SetFromSave_EmptyMapId_IsTreatedAsNoBag()
    {
        // JsonUtility가 되돌린 "빈 주머니" 기본값(mapId="")도 HasBag=false로 다뤄야 한다.
        var registry = new DeathBagRegistry();

        registry.SetFromSave(new DeathBagRecord());

        Assert.IsFalse(registry.HasBag);
    }
}
