using Game.Maps;
using Game.World;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Boot 씬에 하나 둔다. 서비스를 정해진 순서(시계 → 맵 → 나머지)로 GameServices에 등록하고
    /// 준비 완료를 알린다 (ARCHITECTURE.md 2.3, 3.1).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private MapLoader mapLoader;

        private void Awake()
        {
            if (worldClock == null) Debug.LogError("[GameBootstrap] worldClock 참조가 비어 있습니다.", this);
            if (mapLoader == null) Debug.LogError("[GameBootstrap] mapLoader 참조가 비어 있습니다.", this);

            GameServices.RegisterWorldClock(worldClock);
            GameServices.RegisterMapLoader(mapLoader);
            GameServices.NotifyReady();
        }
    }
}
