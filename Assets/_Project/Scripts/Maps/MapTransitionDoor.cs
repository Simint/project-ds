using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 문 상호작용으로 맵을 옮긴다. 인접 지역·실내·층 이동이 모두 이 컴포넌트를 쓰고
    /// travelMinutes는 0이다 (ARCHITECTURE.md 3.3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MapTransitionDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private MapDefinition targetMap;
        [SerializeField] private string targetSpawnPointId = "";
        [SerializeField] private int travelMinutes;

        public string InteractPrompt => targetMap != null ? targetMap.DisplayName : "";

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
            _ = loader.Travel(targetMap, targetSpawnPointId, travelMinutes);
        }
    }
}
