using System.Collections.Generic;
using Game.Core;
using Game.Player;
using Game.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 타이틀 화면 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위 "새 게임 화면"). '이어하기'(저장이 있을 때만)와 '새 게임'을 고른다.
    /// 새 게임이면 난이도와 계절 길이를 정하고, 저장이 있으면 덮어쓸지 한 번 더 묻는다. 고르기 전까지 입력을 잠그고 시간은
    /// 멈춰 있다(GameBootstrap). 버튼은 패드·키보드로 고를 수 있게 처음 버튼을 선택해 둔다.
    /// </summary>
    public sealed class TitleScreenUI : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private PlayerInputReader inputReader;

        [Header("첫 화면")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;

        [Header("새 게임 설정")]
        [SerializeField] private GameObject newGamePanel;
        [SerializeField] private RectTransform difficultyList;
        [SerializeField] private RectTransform seasonList;
        [SerializeField] private Button optionTemplate;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        [Header("덮어쓰기 확인")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private readonly List<Button> spawned = new List<Button>();
        private readonly List<Button> difficultyButtons = new List<Button>();
        private readonly List<Button> seasonButtons = new List<Button>();
        private DifficultyDefinition selectedDifficulty;
        private int selectedSeasonDays;
        // 옵션을 고른 뒤 다시 그릴 때 커서를 그 옵션에 남겨 둔다(패드로 이어서 고르기 편하게). -1이면 시작 버튼.
        private int focusDifficultyIndex = -1;
        private int focusSeasonIndex = -1;

        private void Awake()
        {
            if (bootstrap == null) Debug.LogError("[TitleScreenUI] bootstrap 참조가 비어 있습니다.", this);
            if (worldClock == null) Debug.LogError("[TitleScreenUI] worldClock 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[TitleScreenUI] inputReader 참조가 비어 있습니다.", this);
            if (mainPanel == null || newGamePanel == null || confirmPanel == null) Debug.LogError("[TitleScreenUI] 패널 참조가 비어 있습니다.", this);
            if (optionTemplate != null) optionTemplate.gameObject.SetActive(false);
        }

        private void Start()
        {
            inputReader?.SetLocked(this, true);
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);
            if (newGameButton != null) newGameButton.onClick.AddListener(ShowNewGame);
            if (startButton != null) startButton.onClick.AddListener(HandleStart);
            if (backButton != null) backButton.onClick.AddListener(ShowMain);
            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(StartNewGame);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(ShowNewGame);
            ShowMain();
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.UICancelPressed += HandleCancel;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.UICancelPressed -= HandleCancel;
            inputReader.SetLocked(this, false);
        }

        /// <summary>취소(Esc·패드 B): 확인창 → 새 게임 설정, 새 게임 설정 → 첫 화면.</summary>
        private void HandleCancel()
        {
            if (confirmPanel != null && confirmPanel.activeSelf) ShowNewGame();
            else if (newGamePanel != null && newGamePanel.activeSelf) ShowMain();
        }

        private void ShowMain()
        {
            SetPanels(main: true, newGame: false, confirm: false);
            bool hasSave = bootstrap != null && bootstrap.HasSave;
            if (continueButton != null) continueButton.interactable = hasSave;
            Select(hasSave ? continueButton : newGameButton);
        }

        private void ShowNewGame()
        {
            SetPanels(main: false, newGame: true, confirm: false);
            DifficultyDatabase database = bootstrap != null ? bootstrap.Difficulties : null;
            if (selectedDifficulty == null && database != null) selectedDifficulty = database.Default;
            if (selectedSeasonDays <= 0 && worldClock != null) selectedSeasonDays = worldClock.DefaultSeasonLengthDays;
            RebuildOptions(database);
            Button focus = null;
            if (focusDifficultyIndex >= 0 && focusDifficultyIndex < difficultyButtons.Count) focus = difficultyButtons[focusDifficultyIndex];
            else if (focusSeasonIndex >= 0 && focusSeasonIndex < seasonButtons.Count) focus = seasonButtons[focusSeasonIndex];
            focusDifficultyIndex = -1;
            focusSeasonIndex = -1;
            Select(focus != null ? focus : startButton);
        }

        private void RebuildOptions(DifficultyDatabase database)
        {
            foreach (Button b in spawned)
                if (b != null) Destroy(b.gameObject);
            spawned.Clear();
            difficultyButtons.Clear();
            seasonButtons.Clear();

            if (database != null)
                foreach (DifficultyDefinition difficulty in database.Difficulties)
                {
                    if (difficulty == null) continue;
                    DifficultyDefinition captured = difficulty;
                    int index = difficultyButtons.Count;
                    difficultyButtons.Add(AddOption(difficultyList, difficulty.DisplayName, difficulty == selectedDifficulty,
                        () => { selectedDifficulty = captured; focusDifficultyIndex = index; ShowNewGame(); }));
                }

            if (worldClock != null)
                foreach (int days in worldClock.SeasonLengthOptionsDays)
                {
                    int captured = days;
                    int index = seasonButtons.Count;
                    seasonButtons.Add(AddOption(seasonList, $"계절 {days}일", days == selectedSeasonDays,
                        () => { selectedSeasonDays = captured; focusSeasonIndex = index; ShowNewGame(); }));
                }

            if (summaryText != null)
                summaryText.text = $"난이도: {(selectedDifficulty != null ? selectedDifficulty.DisplayName : "-")}   계절 길이: {selectedSeasonDays}일";
        }

        private Button AddOption(RectTransform parent, string label, bool selected, UnityEngine.Events.UnityAction onClick)
        {
            if (optionTemplate == null || parent == null) return null;
            Button button = Instantiate(optionTemplate, parent);
            button.gameObject.SetActive(true);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = selected ? $"▶ {label}" : label;
            button.onClick.AddListener(onClick);
            spawned.Add(button);
            return button;
        }

        private void HandleContinue()
        {
            if (bootstrap == null || !bootstrap.HasSave) return;
            Close();
            bootstrap.ContinueGame();
        }

        private void HandleStart()
        {
            if (bootstrap != null && bootstrap.HasSave)
            {
                SetPanels(main: false, newGame: false, confirm: true);
                Select(confirmNoButton);
                return;
            }
            StartNewGame();
        }

        private void StartNewGame()
        {
            if (bootstrap == null) return;
            Close();
            bootstrap.StartNewGame(selectedDifficulty, selectedSeasonDays);
        }

        private void Close()
        {
            SetPanels(main: false, newGame: false, confirm: false);
            inputReader?.SetLocked(this, false);
            gameObject.SetActive(false);
        }

        private void SetPanels(bool main, bool newGame, bool confirm)
        {
            if (mainPanel != null) mainPanel.SetActive(main);
            if (newGamePanel != null) newGamePanel.SetActive(newGame);
            if (confirmPanel != null) confirmPanel.SetActive(confirm);
        }

        private static void Select(Button button)
        {
            if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
