using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 설치된 구조물 하나의 런타임 식별자이자 저장 상태의 공용 칠판 (ARCHITECTURE.md 4.5, add-structure
    /// 스킬 2절). PersistentId는 쓰지 않는다 — 설치·철거로 런타임에 생기고 없어지므로 셀 좌표 + 정의
    /// id로 저장한다(BuildGrid가 담당). UntilMinute·Extra는 뜻이 구조물마다 달라서(화로: 연료가 다 타는
    /// 시각, 보관함: 내용물 JSON) 같이 놓인 Stove·StorageContainer 등이 직접 읽고 쓴다. 이 값들은 Awake
    /// 순서에 기대지 않도록 쓰는 쪽에서 캐시하지 않고 그때그때 읽는다.
    /// </summary>
    public sealed class StructureInstance : MonoBehaviour
    {
        public StructureDefinition Definition { get; private set; }
        public Vector2Int Cell { get; private set; }

        /// <summary>뜻은 구조물마다 다르다(문: 열림 여부, 그 외: 쓰지 않으면 0). ObjectStateRecord.State와
        /// 같은 역할이다.</summary>
        public int State { get; set; }
        public long UntilMinute { get; set; }
        public string Extra { get; set; }

        public void Initialize(StructureDefinition definition, Vector2Int cell)
        {
            Definition = definition;
            Cell = cell;
        }
    }
}
