using Game.Core;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>맵 하나의 정의 (ARCHITECTURE.md 3.2). 씬·해금·이동 시간 등 바뀌지 않는 값만 담는다.</summary>
    [CreateAssetMenu(menuName = "Game/Maps/Map Definition")]
    public sealed class MapDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private string sceneName = "";
        [SerializeField] private MapKind kind = MapKind.Region;
        [SerializeField] private bool isIndoor;
        [SerializeField] private float baseRadiation;
        [SerializeField] private int travelMinutesFromHub;
        [SerializeField] private bool unlockedByDefault;

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public MapKind Kind => kind;
        public bool IsIndoor => isIndoor;
        public float BaseRadiation => baseRadiation;
        public int TravelMinutesFromHub => travelMinutesFromHub;
        public bool UnlockedByDefault => unlockedByDefault;
    }
}
