using UnityEngine;

namespace Game.Player
{
    /// <summary>후각 탐지 수치와 흔적 표시 (docs/design/GAME_DESIGN.md 3.2). 실제 값은 비공개 Data/ 에셋에만 둔다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Scent Settings")]
    public sealed class ScentSettings : ScriptableObject
    {
        [SerializeField] [Min(0f)] private float cooldownSeconds = 0f;
        [Tooltip("쓸 때마다 줄어드는 허기.")]
        [SerializeField] [Min(0f)] private float hungerCost = 0f;
        [SerializeField] [Min(0f)] private float radiusTiles = 0f;
        [Tooltip("흔적이 보이는 시간(초).")]
        [SerializeField] [Min(0f)] private float displaySeconds = 0f;
        [SerializeField] [Min(1)] private int maxMarkers = 1;

        [Header("흔적 표시 (Effects 레이어)")]
        [SerializeField] private Sprite markerSprite;
        [SerializeField] private Color digSpotColor = Color.white;
        [SerializeField] private Color itemColor = Color.white;
        [SerializeField] private Color enemyColor = Color.white;

        public float CooldownSeconds => cooldownSeconds;
        public float HungerCost => hungerCost;
        public float RadiusTiles => radiusTiles;
        public float DisplaySeconds => displaySeconds;
        public int MaxMarkers => maxMarkers;
        public Sprite MarkerSprite => markerSprite;

        public Color ColorFor(ScentKind kind) => kind switch
        {
            ScentKind.DigSpot => digSpotColor,
            ScentKind.Enemy => enemyColor,
            _ => itemColor
        };
    }
}
