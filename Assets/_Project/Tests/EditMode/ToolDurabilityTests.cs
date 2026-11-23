using Game.Inventory;
using NUnit.Framework;

public sealed class ToolDurabilityTests
{
    [Test]
    public void TryAdd_WithDurability_SetsSlotDurability()
    {
        var model = new InventoryModel(5);

        model.TryAdd("axe", 1, maxStackSize: 1, unitWeight: 1f, spoilAtMinute: 0, durability: 20);

        Assert.AreEqual(20, model.Slots[0].Durability);
    }

    [Test]
    public void DamageTool_ReducesDurabilityAndClampsAtZero()
    {
        var model = new InventoryModel(5);
        model.TryAdd("axe", 1, 1, 1f, 0, durability: 10);

        model.DamageTool(0, 7);
        Assert.AreEqual(3, model.Slots[0].Durability);

        model.DamageTool(0, 7);
        Assert.AreEqual(0, model.Slots[0].Durability);
    }

    [Test]
    public void DamageTool_OnSlotWithoutDurability_DoesNothing()
    {
        var model = new InventoryModel(5);
        model.TryAdd("wood_log", 3, 20, 0.5f);

        model.DamageTool(0, 5);

        Assert.AreEqual(-1, model.Slots[0].Durability);
        Assert.AreEqual(3, model.Slots[0].Count);
    }

    [Test]
    public void RepairTool_RestoresToMax()
    {
        var model = new InventoryModel(5);
        model.TryAdd("axe", 1, 1, 1f, 0, durability: 10);
        model.DamageTool(0, 10);
        Assert.AreEqual(0, model.Slots[0].Durability);

        model.RepairTool(0, 20);

        Assert.AreEqual(20, model.Slots[0].Durability);
    }

    [Test]
    public void ToolsAlwaysOccupySeparateSlots_EvenWithMaxStackOne()
    {
        var model = new InventoryModel(5);

        model.TryAdd("axe", 1, 1, 1f, 0, durability: 20);
        model.TryAdd("axe", 1, 1, 1f, 0, durability: 20);

        Assert.AreEqual(2, model.CountOf("axe"));
        Assert.AreEqual(20, model.Slots[0].Durability);
        Assert.AreEqual(20, model.Slots[1].Durability);
    }
}
