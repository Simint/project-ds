using System.Collections.Generic;
using Game.Building;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 건설 모드가 켜져 있는 동안 보이는 구조물 목록 (docs/design/GAME_DESIGN.md 3.1, 8). 버튼을
    /// 누르면 BuildModeController.SelectDefinition을 부른다. 마우스로 직접 누르거나, 패드는 UINavigate/
    /// UISubmit으로 고른다(둘 다 PlayerInputReader의 잠금과 무관하게 동작하는 기존 메뉴 입력이다).
    /// </summary>
    public sealed class BuildMenuUI : MonoBehaviour
    {
        [SerializeField] private BuildModeController buildModeController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Button entryTemplate;

        private readonly List<Button> spawnedEntries = new List<Button>();

        private void Awake()
        {
            if (buildModeController == null) Debug.LogError("[BuildMenuUI] buildModeController 참조가 비어 있습니다.", this);
            if (panelRoot == null) Debug.LogError("[BuildMenuUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[BuildMenuUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[BuildMenuUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (buildModeController != null)
            {
                buildModeController.ActiveChanged += HandleActiveChanged;
                buildModeController.SelectedDefinitionChanged += HandleSelectedChanged;
            }
        }

        private void OnDisable()
        {
            if (buildModeController != null)
            {
                buildModeController.ActiveChanged -= HandleActiveChanged;
                buildModeController.SelectedDefinitionChanged -= HandleSelectedChanged;
            }
        }

        private void HandleActiveChanged(bool active)
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(active);
            if (active) Rebuild();
        }

        private void HandleSelectedChanged(StructureDefinition selected) => Rebuild();

        private void Rebuild()
        {
            foreach (Button entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();

            if (buildModeController == null || entryTemplate == null) return;

            foreach (StructureDefinition definition in buildModeController.Options)
            {
                if (definition == null) continue;

                Button button = Instantiate(entryTemplate, listParent);
                button.gameObject.SetActive(true);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = definition.DisplayName;

                bool selected = definition == buildModeController.SelectedDefinition;
                var colors = button.colors;
                colors.normalColor = selected ? colors.pressedColor : colors.normalColor;
                button.colors = colors;

                StructureDefinition captured = definition;
                button.onClick.AddListener(() => buildModeController.SelectDefinition(captured));

                spawnedEntries.Add(button);
            }
        }
    }
}
