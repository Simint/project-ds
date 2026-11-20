using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// IInteractable 가까이 가면 대상 위에 "[키] 설명"을 띄운다 (docs/design/GAME_DESIGN.md 9.5).
    /// PlayerInteractor가 가장 가까운 대상을 찾을 때마다 위치와 문구를 갱신한다.
    /// </summary>
    public sealed class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text label;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.6f, 0f);

        private RectTransform selfRect;

        private void Awake()
        {
            if (root == null) Debug.LogError("[InteractPromptUI] root 참조가 비어 있습니다.", this);
            if (label == null) Debug.LogError("[InteractPromptUI] label 참조가 비어 있습니다.", this);
            if (worldCamera == null) Debug.LogError("[InteractPromptUI] worldCamera 참조가 비어 있습니다.", this);
            selfRect = root != null ? root.GetComponent<RectTransform>() : null;
            if (root != null) root.SetActive(false);
        }

        public void Show(Vector3 worldPosition, string text)
        {
            if (root == null || label == null || worldCamera == null || selfRect == null) return;
            label.text = text;
            root.SetActive(true);
            selfRect.position = worldCamera.WorldToScreenPoint(worldPosition + worldOffset);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
