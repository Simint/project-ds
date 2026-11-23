using System.Collections.Generic;
using Game.Crafting;
using NUnit.Framework;

public sealed class MaterialSourceAggregatorTests
{
    private sealed class FakeMaterialSource : IMaterialSource
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public void Set(string itemId, int count) => counts[itemId] = count;

        public int CountOf(string itemId) => counts.TryGetValue(itemId, out int count) ? count : 0;

        public int Remove(string itemId, int count)
        {
            int have = CountOf(itemId);
            int take = System.Math.Min(have, count);
            counts[itemId] = have - take;
            return take;
        }
    }

    [Test]
    public void CountOf_SumsBagAndAllStorages()
    {
        var bag = new FakeMaterialSource();
        bag.Set("wood_log", 2);
        var storageA = new FakeMaterialSource();
        storageA.Set("wood_log", 3);
        var storageB = new FakeMaterialSource();
        storageB.Set("wood_log", 5);
        var registry = new StorageRegistry();
        registry.Register(storageA);
        registry.Register(storageB);
        var aggregator = new MaterialSourceAggregator(bag, registry);

        Assert.AreEqual(10, aggregator.CountOf("wood_log"));
    }

    [Test]
    public void Remove_TakesFromStoragesBeforeBag()
    {
        var bag = new FakeMaterialSource();
        bag.Set("wood_log", 10);
        var storage = new FakeMaterialSource();
        storage.Set("wood_log", 3);
        var registry = new StorageRegistry();
        registry.Register(storage);
        var aggregator = new MaterialSourceAggregator(bag, registry);

        int removed = aggregator.Remove("wood_log", 5);

        Assert.AreEqual(5, removed);
        Assert.AreEqual(0, storage.CountOf("wood_log"));
        Assert.AreEqual(8, bag.CountOf("wood_log"));
    }

    [Test]
    public void Remove_ReturnsOnlyWhatWasAvailable()
    {
        var bag = new FakeMaterialSource();
        bag.Set("wood_log", 2);
        var aggregator = new MaterialSourceAggregator(bag, new StorageRegistry());

        int removed = aggregator.Remove("wood_log", 10);

        Assert.AreEqual(2, removed);
        Assert.AreEqual(0, bag.CountOf("wood_log"));
    }

    [Test]
    public void TryConsume_FailsAndRemovesNothing_WhenAnyIngredientIsMissing()
    {
        var bag = new FakeMaterialSource();
        bag.Set("wood_log", 5);
        bag.Set("fiber", 0);
        var aggregator = new MaterialSourceAggregator(bag, new StorageRegistry());
        var ingredients = new[]
        {
            new RecipeIngredient("wood_log", 2),
            new RecipeIngredient("fiber", 1)
        };

        bool ok = aggregator.TryConsume(ingredients);

        Assert.IsFalse(ok);
        Assert.AreEqual(5, bag.CountOf("wood_log"));
    }

    [Test]
    public void TryConsume_RemovesAllIngredients_WhenEverythingIsAvailable()
    {
        var bag = new FakeMaterialSource();
        bag.Set("wood_log", 5);
        bag.Set("fiber", 3);
        var aggregator = new MaterialSourceAggregator(bag, new StorageRegistry());
        var ingredients = new[]
        {
            new RecipeIngredient("wood_log", 2),
            new RecipeIngredient("fiber", 1)
        };

        bool ok = aggregator.TryConsume(ingredients);

        Assert.IsTrue(ok);
        Assert.AreEqual(3, bag.CountOf("wood_log"));
        Assert.AreEqual(2, bag.CountOf("fiber"));
    }
}
