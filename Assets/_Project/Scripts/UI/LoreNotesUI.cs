using System.Collections.Generic;
using Game.Core;
using Game.Lore;
using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 쪽지 읽기 창 겸 쪽지 기록 목록 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위). 쪽지를 집으면 그 쪽지가 펼쳐진
    /// 채로 열리고, N키(패드 십자 위)로 언제든 다시 열어 목록에서 골라 읽는다. 열려 있는 동안 입력을 잠그고,
    /// 같은 키나 취소(Esc)로 닫는다. 목록은 열 때마다 다시 그린다(열 때만 할당, hot path 아님). 패드는 UINavigate(위·아래)로
    /// 목록을 옮기면 고른 쪽지가 바로 펼쳐진다(TravelMenuUI와 같은 방식).
    /// </summary>
    public sealed class LoreNotesUI : MonoBehaviour
    {
        // 스틱을 기울인 채로 있을 때 다음 칸으로 넘어가는 간격(기술 값, 초).
        private const float NavRepeatSeconds = 0.25f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listParent;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private LoreDatabase loreDatabase;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private string emptyListMessage = "아직 주운 쪽지가 없다.";

        private readonly List<Button> spawnedEntries = new List<Button>();
        private readonly List<LoreNoteDefinition> spawnedNotes = new List<LoreNoteDefinition>();
        private int selectedIndex = -1;
        private int openedFrame = -1;
        private float navRepeatTimer;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[LoreNotesUI] panelRoot 참조가 비어 있습니다.", this);
            if (listParent == null) Debug.LogError("[LoreNotesUI] listParent 참조가 비어 있습니다.", this);
            if (entryTemplate == null) Debug.LogError("[LoreNotesUI] entryTemplate 참조가 비어 있습니다.", this);
            else entryTemplate.gameObject.SetActive(false);
            if (titleText == null) Debug.LogError("[LoreNotesUI] titleText 참조가 비어 있습니다.", this);
            if (bodyText == null) Debug.LogError("[LoreNotesUI] bodyText 참조가 비어 있습니다.", this);
            if (loreDatabase == null) Debug.LogError("[LoreNotesUI] loreDatabase 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[LoreNotesUI] inputReader 참조가 비어 있습니다.", this);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.ToggleNotesPressed += HandleTogglePressed;
            inputReader.UICancelPressed += HandleCancelPressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.ToggleNotesPressed -= HandleTogglePressed;
            inputReader.UICancelPressed -= HandleCancelPressed;
            inputReader.SetLocked(this, false);
        }

        private void Update()
        {
            if (!IsOpen || inputReader == null || spawnedEntries.Count == 0) return;
            navRepeatTimer -= Time.deltaTime;
            float nav = inputReader.UINavigateInput;
            if (Mathf.Abs(nav) < 0.5f)
            {
                navRepeatTimer = 0f;
                return;
            }
            if (navRepeatTimer > 0f) return;
            navRepeatTimer = NavRepeatSeconds;
            Select((selectedIndex + (nav > 0f ? -1 : 1) + spawnedEntries.Count) % spawnedEntries.Count);
        }

        /// <summary>방금 주운 쪽지를 펼쳐서 연다.</summary>
        public void ShowNote(LoreNoteDefinition note)
        {
            Open();
            int index = spawnedNotes.IndexOf(note);
            if (index >= 0) Select(index);
            else Display(note);
        }

        private void HandleTogglePressed()
        {
            if (IsOpen) { Close(); return; }
            if (inputReader != null && inputReader.IsLockedByOthers(this)) return;
            Open();
            if (spawnedEntries.Count > 0) Select(spawnedEntries.Count - 1);
            else Display(null);
        }

        private void HandleCancelPressed()
        {
            // 패드는 Interact와 취소가 같은 버튼(East)이라, 쪽지를 집은 바로 그 입력에 닫히지 않게 한다(TravelMenuUI와 같은 이유).
            if (!IsOpen || Time.frameCount == openedFrame) return;
            Close();
        }

        private void Open()
        {
            if (panelRoot == null) return;
            RebuildList();
            panelRoot.SetActive(true);
            openedFrame = Time.frameCount;
            navRepeatTimer = 0f;
            inputReader?.SetLocked(this, true);
        }

        private void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            inputReader?.SetLocked(this, false);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= spawnedEntries.Count) return;
            selectedIndex = index;
            Display(spawnedNotes[index]);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(spawnedEntries[index].gameObject);
        }

        private void Display(LoreNoteDefinition note)
        {
            if (titleText != null) titleText.text = note != null ? note.Title : "";
            if (bodyText != null) bodyText.text = note != null ? note.Body : emptyListMessage;
        }

        private void RebuildList()
        {
            foreach (Button entry in spawnedEntries)
                if (entry != null) Destroy(entry.gameObject);
            spawnedEntries.Clear();
            spawnedNotes.Clear();
            selectedIndex = -1;

            IReadOnlyList<string> ids = GameServices.LoreCollection?.Ids;
            if (ids == null || loreDatabase == null || entryTemplate == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                LoreNoteDefinition note = loreDatabase.Find(ids[i]);
                if (note == null) continue;
                Button entry = Instantiate(entryTemplate, listParent);
                entry.gameObject.SetActive(true);
                Text label = entry.GetComponentInChildren<Text>();
                if (label != null) label.text = note.Title;
                int index = spawnedEntries.Count;
                entry.onClick.AddListener(() => Select(index));
                spawnedEntries.Add(entry);
                spawnedNotes.Add(note);
            }
        }
    }
}
