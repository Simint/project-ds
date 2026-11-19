using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 맵 씬의 루트 오브젝트 하나. 맵 정의와 카메라 경계(PolygonCollider2D, 트리거)를 묶고
    /// MapLoader가 스폰 지점을 찾는 창구가 된다 (ARCHITECTURE.md 3.1, 3.2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapRoot : MonoBehaviour
    {
        [SerializeField] private MapDefinition definition;
        [SerializeField] private PolygonCollider2D cameraBounds;

        private SpawnPoint[] spawnPoints = System.Array.Empty<SpawnPoint>();

        public MapDefinition Definition => definition;
        public PolygonCollider2D CameraBounds => cameraBounds;

        private void Awake()
        {
            spawnPoints = GetComponentsInChildren<SpawnPoint>(true);
            if (definition == null) Debug.LogError("[MapRoot] definition 참조가 비어 있습니다.", this);
        }

        public SpawnPoint FindSpawnPoint(string spawnPointId)
        {
            foreach (SpawnPoint sp in spawnPoints)
            {
                if (sp != null && sp.SpawnPointId == spawnPointId) return sp;
            }
            return spawnPoints.Length > 0 ? spawnPoints[0] : null;
        }
    }
}
