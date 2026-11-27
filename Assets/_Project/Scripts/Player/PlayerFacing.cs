using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어가 보는 좌우 방향 (docs/design/GAME_DESIGN.md 6.5 "바라보는 방향 앞쪽"). 이동 입력이
    /// 있으면 그 방향을 보고, 멈추면 마지막 방향을 유지한다. 방향은 bodyRenderer.flipX로 보여준다.
    /// artFacesLeft는 그림이 그려진 기준 방향이다(플레이어 시트는 왼쪽을 보고 그렸다).
    /// </summary>
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("그림이 왼쪽을 보고 그려졌으면 켠다. 오른쪽을 볼 때 좌우 반전한다.")]
        [SerializeField] private bool artFacesLeft;

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

            if (bodyRenderer != null) bodyRenderer.flipX = artFacesLeft ? Direction > 0 : Direction < 0;
        }
    }
}
