using System;
using System.Collections.Generic;
using Game.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 은신처 출발 지점에서 뜨는 지역 선택 목록 (ARCHITECTURE.md 3.3). 1단계용 단순 UI:
    /// entryTemplate을 지역 수만큼 복제해 이름·이동 시간을 채운다.
    /// </summary>
    public sealed class TravelMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Button entryTemplate;

        private readonly List<Button> spawnedButtons = new List<Button>();

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[TravelMenuUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[TravelMenuUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[TravelMenuUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Show(IReadOnlyList<MapDefinition> regions, Action<MapDefinition> onSelected)
        {
            if (panelRoot == null || listParent == null || entryTemplate == null) return;

            Clear();
            foreach (MapDefinition region in regions)
            {
                if (region == null) continue;
                Button button = Instantiate(entryTemplate, listParent);
                button.gameObject.SetActive(true);
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = $"{region.DisplayName} ({region.TravelMinutesFromHub}분)";

                MapDefinition captured = region;
                button.onClick.AddListener(() =>
                {
                    Hide();
                    onSelected?.Invoke(captured);
                });
                spawnedButtons.Add(button);
            }
            panelRoot.SetActive(true);
            // VerticalLayoutGroup은 동적으로 추가된 자식의 다음 리빌드까지 기다리지 않는다.
            // 버튼이 폭 0으로 한 프레임 보일 수 있어 바로 강제로 다시 계산한다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(listParent);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            Clear();
        }

        private void Clear()
        {
            foreach (Button button in spawnedButtons)
                if (button != null) Destroy(button.gameObject);
            spawnedButtons.Clear();
        }
    }
}
