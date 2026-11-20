using System;
using System.Collections.Generic;
using Game.Maps;
using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 은신처 출발 지점에서 뜨는 지역 선택 목록 (ARCHITECTURE.md 3.3, docs/design/GAME_DESIGN.md 9.5).
    /// 1단계용 단순 UI: entryTemplate을 지역 수만큼 복제해 이름·이동 시간을 채운다. 마우스뿐 아니라
    /// 키보드(W/S·방향키 + E)·패드(스틱·방향패드 + 확인/취소 버튼)로도 고르고 닫을 수 있다. 열려 있는
    /// 동안은 PlayerInputReader를 잠가 플레이어 이동을 막는다.
    /// </summary>
    public sealed class TravelMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private float navRepeatSeconds = 0.2f;

        private readonly List<Button> spawnedButtons = new List<Button>();
        private int selectedIndex;
        private float navRepeatTimer;
        private int openedFrame = -1;

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[TravelMenuUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[TravelMenuUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[TravelMenuUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);
            if (inputReader == null) Debug.LogError("[TravelMenuUI] inputReader 참조가 비어 있습니다.", this);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.UISubmitPressed += HandleUISubmitPressed;
            inputReader.UICancelPressed += HandleUICancelPressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.UISubmitPressed -= HandleUISubmitPressed;
            inputReader.UICancelPressed -= HandleUICancelPressed;
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf || inputReader == null || spawnedButtons.Count == 0) return;

            navRepeatTimer -= Time.deltaTime;
            float nav = inputReader.UINavigateInput;
            if (Mathf.Abs(nav) < 0.5f)
            {
                navRepeatTimer = 0f;
                return;
            }
            if (navRepeatTimer > 0f) return;

            navRepeatTimer = navRepeatSeconds;
            MoveSelection(nav > 0f ? -1 : 1);
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

            selectedIndex = 0;
            navRepeatTimer = 0f;
            openedFrame = Time.frameCount;
            SelectCurrent();
            inputReader?.SetLocked(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Clear();
            inputReader?.SetLocked(false);
        }

        private void HandleUISubmitPressed()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            // Interact(E)와 같은 물리 키라, 메뉴를 연 바로 그 입력에 첫 항목이 같이 선택되는 걸 막는다.
            if (Time.frameCount == openedFrame) return;
            if (selectedIndex < 0 || selectedIndex >= spawnedButtons.Count) return;
            spawnedButtons[selectedIndex].onClick.Invoke();
        }

        private void HandleUICancelPressed()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            // Interact(패드 East)와 같은 물리 버튼이라, 메뉴를 연 바로 그 입력에 바로 닫히는 걸 막는다.
            if (Time.frameCount == openedFrame) return;
            Hide();
        }

        private void MoveSelection(int delta)
        {
            selectedIndex = (selectedIndex + delta + spawnedButtons.Count) % spawnedButtons.Count;
            SelectCurrent();
        }

        private void SelectCurrent()
        {
            if (EventSystem.current == null || selectedIndex < 0 || selectedIndex >= spawnedButtons.Count) return;
            EventSystem.current.SetSelectedGameObject(spawnedButtons[selectedIndex].gameObject);
        }

        private void Clear()
        {
            foreach (Button button in spawnedButtons)
                if (button != null) Destroy(button.gameObject);
            spawnedButtons.Clear();
        }
    }
}
