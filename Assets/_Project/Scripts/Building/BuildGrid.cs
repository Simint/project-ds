using System.Collections.Generic;
using Game.Core;
using Game.Maps;
using Game.Save;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 은신처 건설의 유일한 창구 (ARCHITECTURE.md 4.5, add-structure 스킬 3절). 셀 점유를
    /// Dictionary&lt;Vector2Int, StructureInstance&gt;로 관리하고, 좌표 변환은 Unity Grid 하나로만 한다.
    /// 은신처 씬은 다른 맵처럼 오갈 때마다 통째로 사라지므로, 현재 상태를 HubStructureRegistry(세션
    /// 동안의 기억 장소)에 그때그때 밀어 넣고 OnEnable에서 거기서 읽어 되돌린다.
    /// </summary>
    public sealed class BuildGrid : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private HubBuildArea buildArea;
        [SerializeField] private StructureDatabase structureDatabase;
        [SerializeField] private Transform structuresParent;

        private readonly Dictionary<Vector2Int, StructureInstance> occupancy = new Dictionary<Vector2Int, StructureInstance>();
        private readonly List<StructureInstance> instances = new List<StructureInstance>();
        private readonly HashSet<Vector2Int> spawnPointCells = new HashSet<Vector2Int>();

        // CanPlace가 건설 모드 Update마다 불리므로(미리보기), FootprintCells는 매번 새 반복자를
        // 할당하지 않고 이 버퍼를 채워 돌려준다 (.claude/rules/csharp.md "Update 안의 ... 할당").
        private readonly List<Vector2Int> footprintBuffer = new List<Vector2Int>();

        public HubBuildArea BuildArea => buildArea;

        private void Awake()
        {
            if (grid == null) Debug.LogError("[BuildGrid] grid 참조가 비어 있습니다.", this);
            if (buildArea == null) Debug.LogError("[BuildGrid] buildArea 참조가 비어 있습니다.", this);
            if (structureDatabase == null) Debug.LogError("[BuildGrid] structureDatabase 참조가 비어 있습니다.", this);
            if (structuresParent == null) Debug.LogError("[BuildGrid] structuresParent 참조가 비어 있습니다.", this);

            // 스폰 지점 칸은 건설 금지(플레이어가 거기 서 있을 때 바로 벽에 막히면 안 된다). 같은 씬(은신처)
            // 안의 다른 오브젝트를 읽는 한 번뿐인 캐시라 Awake에서 해도 안전하다(MapRoot도 같은 방식).
            foreach (SpawnPoint sp in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include))
                spawnPointCells.Add(WorldToCell(sp.transform.position));
        }

        /// <summary>복원은 Start가 아니라 OnEnable에서 한다. MapLoader.Travel은 씬을 비동기로 불러온 뒤
        /// 같은 프레임에서 바로 계속돼(RestoreNewMapState → ... → AutoSave) 그 안에서
        /// GameServices.BuildGrid.SyncNow()를 부를 수 있는데, Start는 그 다음 프레임으로 미뤄지는 반면
        /// OnEnable은 씬 활성화와 같은 프레임에 동기로 불린다(Awake/OnEnable까지는 씬 로드에 포함, Start는
        /// 별도). Start에서 복원했다면, AutoSave의 SyncNow가 아직 비어 있는 instances로 레지스트리를
        /// 먼저 덮어써 버려 복원 자체가 날아가는 버그가 있었다.</summary>
        private void OnEnable()
        {
            GameServices.RegisterBuildGrid(this);

            if (GameServices.HubStructureRegistry == null) return;
            foreach (StructureSaveData data in GameServices.HubStructureRegistry.Current)
                RestoreOne(data);
        }

        private void OnDisable()
        {
            if (GameServices.BuildGrid == (object)this) GameServices.RegisterBuildGrid(null);
            SyncRegistry();
        }

        /// <summary>저장 직전에 바깥(MapLoader)이 라이브 상태를 강제로 레지스트리에 반영시키려 부른다.</summary>
        public void SyncNow() => SyncRegistry();

        public Vector2Int WorldToCell(Vector2 worldPosition) => (Vector2Int)grid.WorldToCell(worldPosition);

        public Vector3 CellToWorld(Vector2Int cell) => grid.CellToWorld((Vector3Int)cell);

        public StructureInstance GetAt(Vector2Int cell) => occupancy.TryGetValue(cell, out StructureInstance instance) ? instance : null;

        public bool CanPlace(StructureDefinition definition, Vector2Int originCell)
        {
            if (definition == null || definition.Prefab == null) return false;
            List<Vector2Int> cells = FootprintCells(originCell, definition.Footprint);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (!buildArea.IsCellAllowed(cell)) return false;
                if (occupancy.ContainsKey(cell)) return false;
                if (spawnPointCells.Contains(cell)) return false;
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
            List<Vector2Int> cells = FootprintCells(instance.Cell, instance.Definition.Footprint);
            for (int i = 0; i < cells.Count; i++)
                occupancy[cells[i]] = instance;
        }

        private void UnregisterOccupancy(StructureInstance instance)
        {
            List<Vector2Int> cells = FootprintCells(instance.Cell, instance.Definition.Footprint);
            for (int i = 0; i < cells.Count; i++)
                occupancy.Remove(cells[i]);
        }

        /// <summary>origin에서 footprint가 덮는 모든 셀. CanPlace가 건설 모드 Update마다 부르므로
        /// (미리보기), 매번 새 컬렉션을 만들지 않고 이 버퍼를 비우고 다시 채워 돌려준다 — 호출한 쪽은
        /// 바로 다 쓰고 버려야 한다(다음 호출에서 내용이 바뀐다).</summary>
        private List<Vector2Int> FootprintCells(Vector2Int origin, Vector2Int footprint)
        {
            footprintBuffer.Clear();
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    footprintBuffer.Add(new Vector2Int(origin.x + x, origin.y + y));
            return footprintBuffer;
        }
    }
}
