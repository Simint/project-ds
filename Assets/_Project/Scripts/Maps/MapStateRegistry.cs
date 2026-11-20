using System.Collections.Generic;
using Game.Core;

namespace Game.Maps
{
    /// <summary>
    /// 맵별 오브젝트 상태(Capture/Restore) 저장소 (ARCHITECTURE.md 3.4). MonoBehaviour 없이 테스트할
    /// 수 있는 순수 C# 클래스다. SaveService가 저장·불러오기 때 내용을 그대로 옮긴다.
    /// </summary>
    public sealed class MapStateRegistry
    {
        private readonly Dictionary<string, MapState> maps = new Dictionary<string, MapState>();

        public IEnumerable<string> MapIds => maps.Keys;

        /// <summary>맵을 떠나기 직전, 그 맵의 모든 IPersistentObject 상태를 기록한다.</summary>
        public void Capture(string mapId, IEnumerable<IPersistentObject> objects, long nowMinute)
        {
            MapState state = GetOrCreate(mapId);
            state.LastVisitedMinute = nowMinute;
            foreach (IPersistentObject obj in objects)
            {
                if (string.IsNullOrEmpty(obj.Id)) continue;
                state.Objects[obj.Id] = obj.Capture();
            }
        }

        /// <summary>
        /// 새 맵에 들어온 직후, 저장된 상태가 있으면 그대로 되돌린다. 방문한 적 없는 맵은 아무것도
        /// 건드리지 않는다(오브젝트가 씬에 놓인 기본값을 그대로 쓴다).
        /// </summary>
        public void Restore(string mapId, IEnumerable<IPersistentObject> objects, long nowMinute)
        {
            if (!maps.TryGetValue(mapId, out MapState state)) return;
            foreach (IPersistentObject obj in objects)
            {
                if (string.IsNullOrEmpty(obj.Id)) continue;
                if (state.Objects.TryGetValue(obj.Id, out ObjectStateRecord record))
                    obj.Restore(record, nowMinute);
            }
        }

        public long GetLastVisitedMinute(string mapId) =>
            maps.TryGetValue(mapId, out MapState state) ? state.LastVisitedMinute : 0;

        public IReadOnlyDictionary<string, ObjectStateRecord> GetObjects(string mapId) =>
            maps.TryGetValue(mapId, out MapState state) ? state.Objects : EmptyObjects;

        /// <summary>저장 파일을 불러올 때 맵 하나를 통째로 채운다.</summary>
        public void SetMapData(string mapId, long lastVisitedMinute, IEnumerable<KeyValuePair<string, ObjectStateRecord>> objects)
        {
            MapState state = GetOrCreate(mapId);
            state.LastVisitedMinute = lastVisitedMinute;
            state.Objects.Clear();
            foreach (KeyValuePair<string, ObjectStateRecord> pair in objects)
                state.Objects[pair.Key] = pair.Value;
        }

        public void Clear() => maps.Clear();

        private static readonly Dictionary<string, ObjectStateRecord> EmptyObjects = new Dictionary<string, ObjectStateRecord>();

        private MapState GetOrCreate(string mapId)
        {
            if (!maps.TryGetValue(mapId, out MapState state))
            {
                state = new MapState();
                maps[mapId] = state;
            }
            return state;
        }
    }
}
