using UnityEngine;

namespace Game.Maps
{
    /// <summary>맵 안에서 고유한 스폰 지점 (ARCHITECTURE.md 3.3). id는 문 이동의 targetSpawnPointId와 맞춘다.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnPointId = "";

        public string SpawnPointId => spawnPointId;
    }
}
