using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어가 보는 좌우 방향 (docs/design/GAME_DESIGN.md 6.5 "바라보는 방향 앞쪽"). 이동 입력이
    /// 있으면 그 방향을 보고, 멈추면 마지막 방향을 유지한다. 그림이 생기기 전까지는
    /// PlayerHands.RefreshBodyColor와 같은 자리에서 bodyRenderer.flipX로만 방향을 보여준다.
    /// </summary>
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private SpriteRenderer bodyRenderer;

        public int Direction { get; private set; } = 1;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[PlayerFacing] inputReader 참조가 비어 있습니다.", this);
            if (bodyRenderer == null) Debug.LogError("[PlayerFacing] bodyRenderer 참조가 비어 있습니다.", this);
        }

        private void Update()
        {
            if (inputReader == null) return;
            float x = inputReader.MoveInput.x;
            if (x > 0.01f) Direction = 1;
            else if (x < -0.01f) Direction = -1;

            if (bodyRenderer != null) bodyRenderer.flipX = Direction < 0;
        }
    }
}
