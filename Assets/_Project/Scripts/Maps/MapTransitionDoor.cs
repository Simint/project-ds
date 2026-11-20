using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 문 상호작용으로 맵을 옮긴다. 은신처⇄Region 이동 시간은 Region 쪽
    /// MapDefinition.TravelMinutesFromHub를 양방향에 그대로 쓰고, 그 외(실내·같은 지역 내)는
    /// 0이다 (ARCHITECTURE.md 3.3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MapTransitionDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private MapDefinition targetMap;
        [SerializeField] private string targetSpawnPointId = "";

        public string InteractPrompt => targetMap != null ? targetMap.DisplayName : "";
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        public void Interact(GameObject interactor)
        {
            if (targetMap == null)
            {
                GameLog.Warning("MapTransitionDoor", $"'{name}' targetMap이 비어 있습니다.", this);
                return;
            }
            MapLoader loader = GameServices.MapLoader;
            if (loader == null)
            {
                GameLog.Warning("MapTransitionDoor", "GameServices.MapLoader가 등록되지 않았습니다.", this);
                return;
            }
            _ = loader.Travel(targetMap, targetSpawnPointId, ResolveTravelMinutes(loader));
        }

        private int ResolveTravelMinutes(MapLoader loader)
        {
            MapDefinition currentMap = loader.CurrentMap;
            if (currentMap == null) return 0;

            bool hubToRegion = currentMap.Kind == MapKind.Hub && targetMap.Kind == MapKind.Region;
            if (hubToRegion) return targetMap.TravelMinutesFromHub;

            bool regionToHub = currentMap.Kind == MapKind.Region && targetMap.Kind == MapKind.Hub;
            if (regionToHub) return currentMap.TravelMinutesFromHub;

            return 0;
        }
    }
}
