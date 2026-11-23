using System;
using System.Collections.Generic;
using Game.World;

namespace Game.Save
{
    /// <summary>
    /// 저장 파일 하나의 내용 (ARCHITECTURE.md 4.10). JsonUtility로 그대로 직렬화하므로 필드는
    /// public이어야 한다 — JsonUtility는 프로퍼티를 저장하지 않는다. 장비 슬롯(charge 포함),
    /// mapUnlocks, lore, deathBag은 그 기능이 생기는 단계에서 추가한다.
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
        public List<StructureSaveData> hubStructures = new List<StructureSaveData>();
    }

    [Serializable]
    public sealed class InventorySlotData
    {
        public string itemId;
        public int count;
        public long spoilAtMinute;
        public int durability = -1;
    }

    /// <summary>은신처에 설치된 구조물 하나 (ARCHITECTURE.md 4.5, add-structure 스킬 5절). PersistentId가
    /// 아니라 셀 좌표 + 정의 id로 저장한다 — 설치·철거로 런타임에 생기고 없어지는 오브젝트라서다.
    /// untilMinute·extra의 뜻은 구조물 종류마다 다르다(화로: 연료가 다 타는 시각, 보관함: 내용물 JSON).</summary>
    [Serializable]
    public sealed class StructureSaveData
    {
        public string definitionId;
        public int cellX;
        public int cellY;
        public int state;
        public long untilMinute;
        public string extra;
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
