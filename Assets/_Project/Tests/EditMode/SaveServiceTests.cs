using System.Collections.Generic;
using System.IO;
using Game.Save;
using NUnit.Framework;

public sealed class SaveServiceTests
{
    private string tempDir;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "game-ds-save-tests-" + System.Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public void Load_ReturnsNull_WhenNoSaveFileExists()
    {
        var service = new SaveService(tempDir);

        WorldSaveData loaded = service.Load();

        Assert.IsNull(loaded);
    }

    [Test]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var service = new SaveService(tempDir);
        var data = new WorldSaveData
        {
            clockTotalMinutes = 1234,
            currentMapId = "map_residential",
            playerX = 5.5f,
            playerY = -1f,
            inventorySlots = new List<InventorySlotData>
            {
                new InventorySlotData { itemId = "wood_log", count = 3 }
            },
            maps = new List<MapSaveData>
            {
                new MapSaveData
                {
                    mapId = "map_hub",
                    lastVisitedMinute = 500,
                    objects = new List<ObjectSaveData>
                    {
                        new ObjectSaveData { persistentId = "abc", state = 1, untilMinute = 999, extra = null }
                    }
                }
            },
            stats = new List<StatSaveData> { new StatSaveData { id = "hp", value = 42.5f } },
            statusEffects = new List<StatusEffectSaveData> { new StatusEffectSaveData { id = "fracture", untilMinute = 0 } }
        };

        service.Save(data);
        WorldSaveData loaded = service.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(1234, loaded.clockTotalMinutes);
        Assert.AreEqual("map_residential", loaded.currentMapId);
        Assert.AreEqual(5.5f, loaded.playerX, 0.001f);
        Assert.AreEqual(1, loaded.inventorySlots.Count);
        Assert.AreEqual("wood_log", loaded.inventorySlots[0].itemId);
        Assert.AreEqual(3, loaded.inventorySlots[0].count);
        Assert.AreEqual(1, loaded.maps.Count);
        Assert.AreEqual("map_hub", loaded.maps[0].mapId);
        Assert.AreEqual(1, loaded.maps[0].objects.Count);
        Assert.AreEqual("abc", loaded.maps[0].objects[0].persistentId);
        Assert.AreEqual(1, loaded.stats.Count);
        Assert.AreEqual("hp", loaded.stats[0].id);
        Assert.AreEqual(42.5f, loaded.stats[0].value, 0.001f);
        Assert.AreEqual(1, loaded.statusEffects.Count);
        Assert.AreEqual("fracture", loaded.statusEffects[0].id);
    }
}
