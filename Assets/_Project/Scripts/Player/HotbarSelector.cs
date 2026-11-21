using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 현재 선택된 단축칸(가방 앞 slotCount개 중 하나). 숫자 키로 직접 고르거나, 마우스 휠·패드
    /// LB/RB로 한 칸씩 옮긴다 (docs/design/GAME_DESIGN.md 3.1). HotbarUI·ItemUseController가 참조한다.
    /// </summary>
    public sealed class HotbarSelector : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private int slotCount = 10;

        public int SelectedIndex { get; private set; }

        public event Action<int> SelectedIndexChanged;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[HotbarSelector] inputReader 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.HotbarSlotSelected += HandleSlotSelected;
            inputReader.HotbarNextPressed += HandleNextPressed;
            inputReader.HotbarPrevPressed += HandlePrevPressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.HotbarSlotSelected -= HandleSlotSelected;
            inputReader.HotbarNextPressed -= HandleNextPressed;
            inputReader.HotbarPrevPressed -= HandlePrevPressed;
        }

        private void Update()
        {
            if (inputReader == null) return;
            float scroll = inputReader.HotbarScrollInput;
            if (scroll > 0.01f) ChangeSelection(-1);
            else if (scroll < -0.01f) ChangeSelection(1);
        }

        private void HandleSlotSelected(int index)
        {
            if (index >= 0 && index < slotCount) SetSelected(index);
        }

        private void HandleNextPressed() => ChangeSelection(1);

        private void HandlePrevPressed() => ChangeSelection(-1);

        private void ChangeSelection(int delta) => SetSelected(((SelectedIndex + delta) % slotCount + slotCount) % slotCount);

        private void SetSelected(int index)
        {
            if (index == SelectedIndex) return;
            SelectedIndex = index;
            SelectedIndexChanged?.Invoke(SelectedIndex);
        }
    }
}
