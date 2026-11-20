using Game.Inventory;
using NUnit.Framework;

public sealed class InventoryModelTests
{
    [Test]
    public void TryAdd_FillsEmptySlot()
    {
        var model = new InventoryModel(5);

        int leftover = model.TryAdd("wood_log", 3, maxStackSize: 20, unitWeight: 0.5f);

        Assert.AreEqual(0, leftover);
        Assert.AreEqual(3, model.CountOf("wood_log"));
        Assert.AreEqual(1.5f, model.TotalWeight, 0.001f);
    }

    [Test]
    public void TryAdd_StacksIntoExistingSlotBeforeNewOne()
    {
        var model = new InventoryModel(5);
        model.TryAdd("wood_log", 3, 20, 0.5f);

        model.TryAdd("wood_log", 4, 20, 0.5f);

        Assert.AreEqual(7, model.CountOf("wood_log"));
        Assert.AreEqual(1, CountUsedSlots(model));
    }

    [Test]
    public void TryAdd_OverflowsIntoNewSlotWhenStackFull()
    {
        var model = new InventoryModel(5);
        model.TryAdd("wood_log", 20, 20, 0.5f);

        int leftover = model.TryAdd("wood_log", 5, 20, 0.5f);

        Assert.AreEqual(0, leftover);
        Assert.AreEqual(25, model.CountOf("wood_log"));
        Assert.AreEqual(2, CountUsedSlots(model));
    }

    [Test]
    public void TryAdd_ReturnsLeftoverWhenInventoryIsFull()
    {
        var model = new InventoryModel(1);
        model.TryAdd("wood_log", 20, 20, 0.5f);

        int leftover = model.TryAdd("wood_log", 5, 20, 0.5f);

        Assert.AreEqual(5, leftover);
        Assert.AreEqual(20, model.CountOf("wood_log"));
    }

    [Test]
    public void Remove_TakesFromSlotAndClearsWhenEmpty()
    {
        var model = new InventoryModel(5);
        model.TryAdd("wood_log", 3, 20, 0.5f);

        int removed = model.Remove("wood_log", 3);

        Assert.AreEqual(3, removed);
        Assert.AreEqual(0, model.CountOf("wood_log"));
        Assert.AreEqual(0f, model.TotalWeight, 0.001f);
    }

    [Test]
    public void Remove_ReturnsOnlyWhatWasAvailable()
    {
        var model = new InventoryModel(5);
        model.TryAdd("wood_log", 2, 20, 0.5f);

        int removed = model.Remove("wood_log", 10);

        Assert.AreEqual(2, removed);
        Assert.AreEqual(0, model.CountOf("wood_log"));
    }

    [Test]
    public void Changed_FiresOnAddAndRemove()
    {
        var model = new InventoryModel(5);
        int changeCount = 0;
        model.Changed += () => changeCount++;

        model.TryAdd("wood_log", 1, 20, 0.5f);
        model.Remove("wood_log", 1);

        Assert.AreEqual(2, changeCount);
    }

    private static int CountUsedSlots(InventoryModel model)
    {
        int used = 0;
        foreach (InventorySlot slot in model.Slots)
            if (slot != null && !slot.IsEmpty) used++;
        return used;
    }
}
