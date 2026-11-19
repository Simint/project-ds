using Game.Core;
using Game.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>현재 맵 id와 게임 내 시각(기본 하루 1440분 기준)을 보여주는 디버그 표시.</summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private const int AssumedDayLengthMinutes = 1440;

        [SerializeField] private Text label;

        private void OnEnable()
        {
            GameServices.Ready += HandleServicesReady;
            if (GameServices.IsReady) HandleServicesReady();
        }

        private void OnDisable()
        {
            GameServices.Ready -= HandleServicesReady;
            if (GameServices.WorldClock != null) GameServices.WorldClock.MinutePassed -= HandleMinutePassed;
            if (GameServices.MapLoader != null) GameServices.MapLoader.MapChanged -= HandleMapChanged;
        }

        private void HandleServicesReady()
        {
            if (GameServices.WorldClock != null) GameServices.WorldClock.MinutePassed += HandleMinutePassed;
            if (GameServices.MapLoader != null) GameServices.MapLoader.MapChanged += HandleMapChanged;
            Refresh();
        }

        private void HandleMinutePassed() => Refresh();

        private void HandleMapChanged(MapDefinition _) => Refresh();

        private void Refresh()
        {
            if (label == null) return;
            MapLoader loader = GameServices.MapLoader;
            string mapId = loader != null && loader.CurrentMap != null ? loader.CurrentMap.Id : "-";
            long minutes = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            int day = (int)(minutes / AssumedDayLengthMinutes) + 1;
            int hour = (int)(minutes / 60 % 24);
            int minute = (int)(minutes % 60);
            label.text = $"map: {mapId}  day {day}  {hour:D2}:{minute:D2}";
        }
    }
}
