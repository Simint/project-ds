using UnityEngine;

namespace Game.Building
{
    /// <summary>빗물 통 수치 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 실제 값은 비공개 Data/Structures/ 에셋에만 둔다.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Rain Barrel Settings")]
    public sealed class RainBarrelSettings : ScriptableObject
    {
        [Tooltip("물병 몇 개 분량까지 담는지.")]
        [SerializeField] [Min(0)] private int capacityBottles = 0;
        [Tooltip("비가 올 때 한 시간에 차는 물병 수.")]
        [SerializeField] [Min(0f)] private float bottlesPerRainHour = 0f;

        public int CapacityBottles => capacityBottles;
        public float BottlesPerRainHour => bottlesPerRainHour;
    }
}
