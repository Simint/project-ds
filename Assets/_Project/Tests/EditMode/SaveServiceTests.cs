using System.Collections.Generic;
using System.IO;
using Game.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    [Test]
    public void Save_LeavesNoTempFileBehind()
    {
        var service = new SaveService(tempDir);
        service.Save(new WorldSaveData { clockTotalMinutes = 1 });

        Assert.IsTrue(File.Exists(Path.Combine(tempDir, "slot0.json")));
        Assert.IsFalse(File.Exists(Path.Combine(tempDir, "slot0.json.tmp")));
    }

    [Test]
    public void Save_Twice_ReplacesThePreviousFileCleanly()
    {
        var service = new SaveService(tempDir);
        service.Save(new WorldSaveData { clockTotalMinutes = 1 });

        service.Save(new WorldSaveData { clockTotalMinutes = 2 });

        Assert.AreEqual(2, service.Load().clockTotalMinutes);
    }

    [Test]
    public void Load_ReturnsNull_WhenFileIsCorruptedInsteadOfThrowing()
    {
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "slot0.json"), "{ 이건 JSON이 아니다 ::: ");
        var service = new SaveService(tempDir);

        WorldSaveData loaded = service.Load();

        Assert.IsNull(loaded);
    }

    [Test]
    public void Load_OldFileWithoutSlotIndex_DefaultsSlotIndexToMinusOne()
    {
        // 버전 3 이전 저장 파일(InventorySlotData에 slotIndex 필드가 없던 시절)을 그대로 흉내 낸다.
        Directory.CreateDirectory(tempDir);
        string legacyJson = "{\"version\":2,\"inventorySlots\":[{\"itemId\":\"wood_log\",\"count\":3,\"spoilAtMinute\":0,\"durability\":-1}]}";
        File.WriteAllText(Path.Combine(tempDir, "slot0.json"), legacyJson);
        var service = new SaveService(tempDir);

        WorldSaveData loaded = service.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(2, loaded.version);
        Assert.AreEqual(1, loaded.inventorySlots.Count);
        Assert.AreEqual(-1, loaded.inventorySlots[0].slotIndex);
    }

    [Test]
    public void Save_ReturnsFalseAndKeepsOldFile_WhenDiskWriteFails()
    {
        var service = new SaveService(tempDir);
        Assert.IsTrue(service.Save(new WorldSaveData { clockTotalMinutes = 1 }));

        // 임시 파일 자리에 폴더를 만들어 두면 WriteAllText가 IOException/UnauthorizedAccessException을 낸다.
        Directory.CreateDirectory(Path.Combine(tempDir, "slot0.json.tmp"));
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Save\] 저장하지 못했습니다"));

        bool saved = service.Save(new WorldSaveData { clockTotalMinutes = 2 });

        Assert.IsFalse(saved);
        Assert.AreEqual(1, service.Load().clockTotalMinutes);
    }
}
