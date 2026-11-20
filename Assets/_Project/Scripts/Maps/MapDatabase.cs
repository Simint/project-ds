using System.Collections.Generic;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>모든 MapDefinition을 id로 찾는 색인. 저장 파일의 currentMapId를 되돌릴 때만 쓴다.</summary>
    [CreateAssetMenu(menuName = "Game/Maps/Map Database")]
    public sealed class MapDatabase : ScriptableObject
    {
        [SerializeField] private MapDefinition[] maps = System.Array.Empty<MapDefinition>();

        private Dictionary<string, MapDefinition> lookup;

        public MapDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out MapDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, MapDefinition>();
            foreach (MapDefinition map in maps)
            {
                if (map == null || string.IsNullOrEmpty(map.Id)) continue;
                if (!lookup.TryAdd(map.Id, map))
                    Debug.LogError($"[MapDatabase] 맵 id '{map.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
