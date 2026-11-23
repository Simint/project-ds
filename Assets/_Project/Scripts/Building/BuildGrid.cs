using System.Collections.Generic;
using Game.Core;
using Game.Save;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 은신처 건설의 유일한 창구 (ARCHITECTURE.md 4.5, add-structure 스킬 3절). 셀 점유를
    /// Dictionary&lt;Vector2Int, StructureInstance&gt;로 관리하고, 좌표 변환은 Unity Grid 하나로만 한다.
    /// 은신처 씬은 다른 맵처럼 오갈 때마다 통째로 사라지므로, 현재 상태를 HubStructureRegistry(세션
    /// 동안의 기억 장소)에 그때그때 밀어 넣고 Start에서 거기서 읽어 되돌린다.
    /// </summary>
    public sealed class BuildGrid : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private HubBuildArea buildArea;
        [SerializeField] private StructureDatabase structureDatabase;
        [SerializeField] private Transform structuresParent;

        private readonly Dictionary<Vector2Int, StructureInstance> occupancy = new Dictionary<Vector2Int, StructureInstance>();
        private readonly List<StructureInstance> instances = new List<StructureInstance>();

        public HubBuildArea BuildArea => buildArea;

        private void Awake()
        {
            if (grid == null) Debug.LogError("[BuildGrid] grid 참조가 비어 있습니다.", this);
            if (buildArea == null) Debug.LogError("[BuildGrid] buildArea 참조가 비어 있습니다.", this);
            if (structureDatabase == null) Debug.LogError("[BuildGrid] structureDatabase 참조가 비어 있습니다.", this);
            if (structuresParent == null) Debug.LogError("[BuildGrid] structuresParent 참조가 비어 있습니다.", this);
        }

        private void Start()
        {
            if (GameServices.HubStructureRegistry == null) return;
            foreach (StructureSaveData data in GameServices.HubStructureRegistry.Current)
                RestoreOne(data);
        }

        private void OnDisable() => SyncRegistry();

        public Vector2Int WorldToCell(Vector2 worldPosition) => (Vector2Int)grid.WorldToCell(worldPosition);

        public Vector3 CellToWorld(Vector2Int cell) => grid.CellToWorld((Vector3Int)cell);

        public StructureInstance GetAt(Vector2Int cell) => occupancy.TryGetValue(cell, out StructureInstance instance) ? instance : null;

        public bool CanPlace(StructureDefinition definition, Vector2Int originCell)
        {
            if (definition == null || definition.Prefab == null) return false;
            foreach (Vector2Int cell in FootprintCells(originCell, definition.Footprint))
            {
                if (!buildArea.IsCellAllowed(cell)) return false;
                if (occupancy.ContainsKey(cell)) return false;
            }
            Vector2 originWorld = CellToWorld(originCell);
            return PlacementRules.CanPlace(definition.Kind, originWorld, definition.Footprint);
        }

        public StructureInstance Place(StructureDefinition definition, Vector2Int originCell)
        {
            if (!CanPlace(definition, originCell)) return null;

            GameObject go = Instantiate(definition.Prefab, CellToWorld(originCell), Quaternion.identity, structuresParent);
            StructureInstance instance = go.GetComponent<StructureInstance>();
            if (instance == null)
            {
                GameLog.Warning("BuildGrid", $"'{definition.Id}' 프리팹에 StructureInstance가 없습니다.", this);
                Destroy(go);
                return null;
            }
            instance.Initialize(definition, originCell);
            RegisterOccupancy(instance);
            instances.Add(instance);
            SyncRegistry();
            return instance;
        }

        /// <summary>그리드에서 지우고 파괴한다. 재료 환불·보관함 내용물 처리는 호출한 쪽(건설 모드)이
        /// GetAt으로 미리 읽은 뒤 맡는다 — BuildGrid는 그리드 사정만 안다.</summary>
        public void Remove(Vector2Int cell)
        {
            if (!occupancy.TryGetValue(cell, out StructureInstance instance)) return;
            UnregisterOccupancy(instance);
            instances.Remove(instance);
            if (instance != null) Destroy(instance.gameObject);
            SyncRegistry();
        }

        private void RestoreOne(StructureSaveData data)
        {
            StructureDefinition def = structureDatabase.Find(data.definitionId);
            if (def == null || def.Prefab == null)
            {
                GameLog.Warning("BuildGrid", $"구조물 id '{data.definitionId}'를 찾지 못했습니다.", this);
                return;
            }
            Vector2Int cell = new Vector2Int(data.cellX, data.cellY);
            GameObject go = Instantiate(def.Prefab, CellToWorld(cell), Quaternion.identity, structuresParent);
            StructureInstance instance = go.GetComponent<StructureInstance>();
            if (instance == null) { Destroy(go); return; }

            instance.Initialize(def, cell);
            instance.State = data.state;
            instance.UntilMinute = data.untilMinute;
            instance.Extra = data.extra;
            RegisterOccupancy(instance);
            instances.Add(instance);
        }

        private void SyncRegistry()
        {
            if (GameServices.HubStructureRegistry == null) return;
            var list = new List<StructureSaveData>(instances.Count);
            foreach (StructureInstance instance in instances)
            {
                if (instance == null || instance.Definition == null) continue;
                list.Add(new StructureSaveData
                {
                    definitionId = instance.Definition.Id,
                    cellX = instance.Cell.x,
                    cellY = instance.Cell.y,
                    state = instance.State,
                    untilMinute = instance.UntilMinute,
                    extra = instance.Extra
                });
            }
            GameServices.HubStructureRegistry.SetCurrent(list);
        }

        private void RegisterOccupancy(StructureInstance instance)
        {
            foreach (Vector2Int cell in FootprintCells(instance.Cell, instance.Definition.Footprint))
                occupancy[cell] = instance;
        }

        private void UnregisterOccupancy(StructureInstance instance)
        {
            foreach (Vector2Int cell in FootprintCells(instance.Cell, instance.Definition.Footprint))
                occupancy.Remove(cell);
        }

        private static IEnumerable<Vector2Int> FootprintCells(Vector2Int origin, Vector2Int footprint)
        {
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    yield return new Vector2Int(origin.x + x, origin.y + y);
        }
    }
}
