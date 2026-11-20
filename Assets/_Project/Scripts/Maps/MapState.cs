using System.Collections.Generic;
using Game.Core;

namespace Game.Maps
{
    /// <summary>맵 하나의 저장 상태: persistentId별 기록 + 마지막 방문 시각 (ARCHITECTURE.md 3.4).</summary>
    public sealed class MapState
    {
        public long LastVisitedMinute { get; set; }
        public Dictionary<string, ObjectStateRecord> Objects { get; } = new Dictionary<string, ObjectStateRecord>();
    }
}
