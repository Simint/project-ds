using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 건설 미리보기 — 선택한 구조물의 스프라이트를 반투명하게, 놓을 수 있으면 초록·없으면 빨강 틴트로
    /// 보여준다 (add-structure 스킬 4절). 콜라이더는 두지 않는다.
    /// </summary>
    public sealed class PlacementPreview : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BuildSettings settings;

        private void Awake()
        {
            if (spriteRenderer == null) Debug.LogError("[PlacementPreview] spriteRenderer 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlacementPreview] settings 참조가 비어 있습니다.", this);
            Hide();
        }

        /// <summary>visualLocalOffset은 실제 프리팹의 Visual 자식이 루트에서 떨어진 만큼이다
        /// (add-structure 스킬 1절, footprint.x*0.5). 이걸 더하지 않으면 2칸 이상인 구조물은 미리보기가
        /// 실제로 놓일 자리보다 반 칸 어긋나 보인다.</summary>
        public void Refresh(Sprite sprite, Vector3 worldPosition, Vector3 visualLocalOffset, bool canPlace)
        {
            if (spriteRenderer == null) return;
            spriteRenderer.enabled = sprite != null;
            if (sprite == null) return;

            spriteRenderer.sprite = sprite;
            transform.position = worldPosition + visualLocalOffset;
            Color tint = canPlace ? settings.PreviewAllowedColor : settings.PreviewBlockedColor;
            spriteRenderer.color = tint;
        }

        public void Hide()
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
    }
}
