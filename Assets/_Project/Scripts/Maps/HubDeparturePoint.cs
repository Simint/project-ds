using System.Collections.Generic;
using Game.Core;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 은신처의 출발 지점. 상호작용하면 해금된 Region 목록(이름·이동 시간)을 보여주고,
    /// 고른 곳으로 MapDefinition.TravelMinutesFromHub만큼 시간을 흘려 이동한다 (ARCHITECTURE.md 3.3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HubDeparturePoint : MonoBehaviour, IInteractable
    {
        private const string FromHubSpawnPointId = "from_hub";

        [SerializeField] private MapDefinition[] regions = System.Array.Empty<MapDefinition>();

        private readonly List<MapDefinition> unlockedRegions = new List<MapDefinition>();

        public string InteractPrompt => "떠나기";

        public void Interact(GameObject interactor)
        {
            TravelMenuUI menu = GameServices.TravelMenu;
            if (menu == null)
            {
                GameLog.Warning("HubDeparturePoint", "GameServices.TravelMenu가 등록되지 않았습니다.", this);
                return;
            }

            unlockedRegions.Clear();
            foreach (MapDefinition region in regions)
                if (region != null && region.UnlockedByDefault) unlockedRegions.Add(region);

            menu.Show(unlockedRegions, HandleRegionSelected);
        }

        private void HandleRegionSelected(MapDefinition region)
        {
            if (region == null) return;
            MapLoader loader = GameServices.MapLoader;
            if (loader == null)
            {
                GameLog.Warning("HubDeparturePoint", "GameServices.MapLoader가 등록되지 않았습니다.", this);
                return;
            }
            _ = loader.Travel(region, FromHubSpawnPointId, region.TravelMinutesFromHub);
        }
    }
}
