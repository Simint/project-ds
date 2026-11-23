using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 은신처에서 건설이 허용된 셀 범위 (ARCHITECTURE.md 4.5). 처음엔 baseAreas만 열려 있고,
    /// HubObstacle을 치우면 unlockedAreas가 늘어난다 — 이 둘 중 하나에라도 들어가면 설치할 수 있다.
    /// unlockedAreas는 저장하지 않는다. 대신 HubObstacle 각각이 스스로 저장되어(PersistentId), 씬을
    /// 다시 불러올 때 치워진 장애물이 Restore에서 다시 UnlockArea를 불러 똑같은 범위를 연다.
    /// </summary>
    public sealed class HubBuildArea : MonoBehaviour
    {
        [SerializeField] private RectInt[] baseAreas = System.Array.Empty<RectInt>();

        private readonly List<RectInt> unlockedAreas = new List<RectInt>();

        public bool IsCellAllowed(Vector2Int cell)
        {
            foreach (RectInt rect in baseAreas)
                if (rect.Contains(cell)) return true;
            foreach (RectInt rect in unlockedAreas)
                if (rect.Contains(cell)) return true;
            return false;
        }

        public void UnlockArea(RectInt area)
        {
            if (!unlockedAreas.Contains(area)) unlockedAreas.Add(area);
        }
    }
}
