using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 이동 상태 머신 (ARCHITECTURE.md 4.2). 걷기/달리기는 Grounded 상태 하나가 프리셋을
    /// 바꿔가며 처리하고, 점프 상승/낙하는 중력 계산이 달라 따로 상태를 둔다. 공격·채집·상호작용
    /// 상태는 해당 시스템이 생기는 단계(3·6·7단계)에서 추가한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, 0.05f);
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);

        private readonly IPlayerMotionState groundedState = new GroundedMotionState();
        private readonly IPlayerMotionState jumpingState = new JumpingMotionState();
        private readonly IPlayerMotionState fallingState = new FallingMotionState();

        private Rigidbody2D body;
        private IPlayerMotionState currentState;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool jumpCutRequested;

        public Rigidbody2D Body => body;
        public PlayerMovementSettings Settings => settings;
        public bool IsGrounded { get; private set; }
        public float HorizontalInput => inputReader != null ? inputReader.MoveInput.x : 0f;

        internal IPlayerMotionState GroundedState => groundedState;
        internal IPlayerMotionState FallingState => fallingState;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (inputReader == null) Debug.LogError("[PlayerController] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerController] hands 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerController] settings 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.JumpPressed += HandleJumpPressed;
            inputReader.JumpReleased += HandleJumpReleased;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.JumpPressed -= HandleJumpPressed;
            inputReader.JumpReleased -= HandleJumpReleased;
        }

        private void Start()
        {
            ChangeState(groundedState);
        }

        private void FixedUpdate()
        {
            if (settings == null) return;

            IsGrounded = Physics2D.OverlapBox((Vector2)transform.position + groundCheckOffset, groundCheckSize, 0f, GameLayers.GroundCheckMask);

            // currentState == groundedState(실제로 땅에 서 있는 상태)일 때만 코요테 타임을 다시 채운다.
            // IsGrounded만 보면 점프 상승 중 OneWayPlatform을 통과하는 짧은 구간에도 발판 콜라이더와
            // 겹쳐 true가 되어(GroundCheckMask에 OneWayPlatform 포함), 공중에서 코요테 타임이 재충전될 수 있다.
            bool actuallyGrounded = IsGrounded && currentState == groundedState;
            coyoteTimer = actuallyGrounded ? settings.CoyoteTimeSeconds : coyoteTimer - Time.fixedDeltaTime;
            jumpBufferTimer -= Time.fixedDeltaTime;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && currentState != jumpingState)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                body.linearVelocity = new Vector2(body.linearVelocity.x, settings.JumpVelocity);
                ChangeState(jumpingState);
            }

            // 점프가 이번 프레임에 막 시작했을 수도 있으니, 발동 체크 다음에 컷을 적용한다.
            // 그래야 누르자마자 바로 뗀 경우에도(버퍼링된 점프가 같은 프레임에 발동) 가변 점프가 먹힌다.
            if (jumpCutRequested)
            {
                jumpCutRequested = false;
                if (body.linearVelocity.y > 0f)
                    body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * settings.JumpCutMultiplier);
            }

            currentState.Tick(this, Time.fixedDeltaTime);
        }

        public MovementPreset ResolvePreset()
        {
            if (inputReader == null || !inputReader.RunHeld) return settings.Walk;
            return hands != null && hands.Mode == HandMode.Beast ? settings.BeastRun : settings.Run;
        }

        internal void ChangeState(IPlayerMotionState next)
        {
            currentState = next;
            currentState.Enter(this);
        }

        private void HandleJumpPressed()
        {
            jumpBufferTimer = settings != null ? settings.JumpBufferSeconds : 0f;
        }

        private void HandleJumpReleased()
        {
            jumpCutRequested = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
        }
    }
}
