using UnityEngine;

namespace Game.Building
{
    /// <summary>건설 모드 수치 (docs/design/GAME_DESIGN.md 3.1, 8). 전부 임시값, 플레이하며 조절한다.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Build Settings")]
    public sealed class BuildSettings : ScriptableObject
    {
        [SerializeField] private float cursorMoveSpeedTiles;
        [SerializeField] private Color previewAllowedColor = Color.white;
        [SerializeField] private Color previewBlockedColor = Color.white;

        public float CursorMoveSpeedTiles => cursorMoveSpeedTiles;
        public Color PreviewAllowedColor => previewAllowedColor;
        public Color PreviewBlockedColor => previewBlockedColor;
    }
}
