using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 임시 이동: 좌우로만 걷는다. 점프·가속도·코요테 타임 등은 2단계(tune-movement)에서
    /// 상태 머신(PlayerController)과 PlayerMovementSettings로 교체한다 (ARCHITECTURE.md 6장, 4.2).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private float moveSpeed = 4f;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (inputReader == null) Debug.LogError("[PlayerMotor] inputReader 참조가 비어 있습니다.", this);
        }

        private void FixedUpdate()
        {
            float x = inputReader != null ? inputReader.MoveInput.x : 0f;
            body.linearVelocity = new Vector2(x * moveSpeed, body.linearVelocity.y);
        }
    }
}
