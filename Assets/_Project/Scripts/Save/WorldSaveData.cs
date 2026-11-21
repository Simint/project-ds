using System;
using System.Collections.Generic;
using Game.World;

namespace Game.Save
{
    /// <summary>
    /// 저장 파일 하나의 내용 (ARCHITECTURE.md 4.10). JsonUtility로 그대로 직렬화하므로 필드는
    /// public이어야 한다 — JsonUtility는 프로퍼티를 저장하지 않는다. 인벤토리 durability/charge,
    /// hub, mapUnlocks, lore, deathBag은 그 기능이 생기는 단계에서 추가한다.
    /// </summary>
    [Serializable]
    public sealed class WorldSaveData
    {
        public int version = 1;
        public WorldSettings settings;
        public long clockTotalMinutes;
        public string currentMapId = "";
        public float playerX;
        public float playerY;
        public List<InventorySlotData> inventorySlots = new List<InventorySlotData>();
        public List<MapSaveData> maps = new List<MapSaveData>();
        public List<StatSaveData> stats = new List<StatSaveData>();
        public List<StatusEffectSaveData> statusEffects = new List<StatusEffectSaveData>();
    }

    [Serializable]
    public sealed class InventorySlotData
    {
        public string itemId;
        public int count;
        public long spoilAtMinute;
    }

    [Serializable]
    public sealed class StatSaveData
    {
        public string id;
        public float value;
    }

    [Serializable]
    public sealed class StatusEffectSaveData
    {
        public string id;
        public long untilMinute;
    }

    [Serializable]
    public sealed class MapSaveData
    {
        public string mapId;
        public long lastVisitedMinute;
        public List<ObjectSaveData> objects = new List<ObjectSaveData>();
    }

    [Serializable]
    public sealed class ObjectSaveData
    {
        public string persistentId;
        public int state;
        public long untilMinute;
        public string extra;
    }
}
