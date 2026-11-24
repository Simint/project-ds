using Game.Core;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 건설 설치 조건 판정을 종류별로 한곳에 모은다 (add-structure 스킬 3절). 프리팹이나
    /// BuildModeController에 흩어 놓지 않는다. 1유닛 = 1셀 기준이라 셀 크기는 항상 1이다(PPU는 이미지
    /// 해상도일 뿐이라 이 단위 체계와 무관하다, RENDERING_RULES.md 1장).
    /// </summary>
    public static class PlacementRules
    {
        /// <summary>footprint가 차지할 사각형 전체가 비어 있는지 (지형·다른 구조물·플레이어·몬스터,
        /// GameLayers.BuildBlockingMask).</summary>
        public static bool IsAreaClear(Vector2 originWorld, Vector2Int footprint)
        {
            Vector2 size = new Vector2(footprint.x, footprint.y);
            Vector2 center = originWorld + size * 0.5f;
            return !Physics2D.OverlapBox(center, size * 0.95f, 0f, GameLayers.BuildBlockingMask);
        }

        /// <summary>종류별 지지 조건. Solid/Platform/Prop/Door는 아래 칸이 받쳐져야 하고, BackWall은
        /// 인접한 BackWall·Solid가 있어야 한다 — BackWall은 이번 단계에서 쓰지 않아 항상 통과시킨다.</summary>
        public static bool HasSupport(StructureKind kind, Vector2 originWorld, Vector2Int footprint)
        {
            if (kind == StructureKind.BackWall) return true;

            Vector2 belowCenter = originWorld + new Vector2(footprint.x * 0.5f, -0.05f);
            Vector2 belowSize = new Vector2(footprint.x * 0.9f, 0.1f);
            return Physics2D.OverlapBox(belowCenter, belowSize, 0f, GameLayers.GroundCheckMask);
        }

        public static bool CanPlace(StructureKind kind, Vector2 originWorld, Vector2Int footprint) =>
            IsAreaClear(originWorld, footprint) && HasSupport(kind, originWorld, footprint);
    }
}
