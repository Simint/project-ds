using System.Collections.Generic;
using Game.Core;
using Game.Survival;
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
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, 0.05f);
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);

        private readonly IPlayerMotionState groundedState = new GroundedMotionState();
        private readonly IPlayerMotionState jumpingState = new JumpingMotionState();
        private readonly IPlayerMotionState fallingState = new FallingMotionState();
        private readonly IPlayerMotionState knockbackState = new KnockbackMotionState();

        // 출처(골절, 무게 등)별 이동·점프 배율. 모두 곱해서 쓴다 (add-survival-stat/상태 이상 연결용 자리,
        // ARCHITECTURE.md 4.2). Update가 아니라 상태 이상이 걸리거나 풀릴 때만 바뀌므로 매 프레임
        // Dictionary.Values를 도는 것도 할당이 없어 허용한다.
        private readonly Dictionary<string, float> speedMultipliers = new Dictionary<string, float>();
        private readonly Dictionary<string, float> jumpMultipliers = new Dictionary<string, float>();

        private Rigidbody2D body;
        private IPlayerMotionState currentState;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool jumpCutRequested;
        private Vector2 pendingKnockbackVelocity;
        private float pendingStunSeconds;

        public Rigidbody2D Body => body;
        public PlayerMovementSettings Settings => settings;
        public bool IsGrounded { get; private set; }

        /// <summary>실제로 땅 위 이동 상태인지. IsGrounded(겹침 판정)는 점프 상승 중 OneWayPlatform을
        /// 통과할 때도 잠깐 true가 되므로, 표시(PlayerAnimator)는 이 값을 쓴다.</summary>
        public bool IsInGroundedState => currentState == groundedState;
        public float HorizontalInput => inputReader != null ? inputReader.MoveInput.x : 0f;

        internal IPlayerMotionState GroundedState => groundedState;
        internal IPlayerMotionState FallingState => fallingState;

        /// <summary>적에게 맞아 밀려날 때 (docs/design/GAME_DESIGN.md 6.5 넉백). 입력을 무시하는
        /// KnockbackMotionState로 잠깐 넘어간다 — Grounded가 매 FixedUpdate 수평 속도를 입력대로
        /// 덮어쓰기 때문에, 그냥 velocity만 바꿔서는 다음 틱에 바로 지워진다.</summary>
        public void ApplyKnockback(Vector2 velocity, float stunSeconds)
        {
            pendingKnockbackVelocity = velocity;
            pendingStunSeconds = stunSeconds;
            // 맞기 직전에 누른 점프(버퍼)나 땅에서 남은 코요테 타임으로 넉백이 바로 점프로 취소되지
            // 않게 비운다. 넉백 중에 새로 누른 점프는 아래 FixedUpdate 조건이 막는다.
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCutRequested = false;
            ChangeState(knockbackState);
        }

        internal Vector2 ConsumePendingKnockbackVelocity() => pendingKnockbackVelocity;
        internal float ConsumePendingStunSeconds() => pendingStunSeconds;

        public float SpeedMultiplier => Multiply(speedMultipliers);
        public float JumpMultiplier => Multiply(jumpMultipliers);

        /// <summary>낙하가 끝나 땅에 닿을 때마다 (떨어진 거리, 타일 단위)를 발행한다. FallDamage가 구독한다.</summary>
        public event System.Action<float> Landed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (inputReader == null) Debug.LogError("[PlayerController] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerController] hands 참조가 비어 있습니다.", this);
            if (survival == null) Debug.LogError("[PlayerController] survival 참조가 비어 있습니다.", this);
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

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && currentState != jumpingState && currentState != knockbackState)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                body.linearVelocity = new Vector2(body.linearVelocity.x, settings.JumpVelocity * JumpMultiplier);
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
            if (inputReader != null && inputReader.CrouchHeld) return settings.Crouch; // 앉은 채로는 달릴 수 없다 (6.5)
            if (inputReader == null || !inputReader.RunHeld) return settings.Walk;
            if (survival != null && !survival.HasStamina) return settings.Walk; // 스태미나가 바닥이면 달릴 수 없다
            return hands != null && hands.Mode == HandMode.Beast ? settings.BeastRun : settings.Run;
        }

        internal void ChangeState(IPlayerMotionState next)
        {
            currentState = next;
            currentState.Enter(this);
        }

        /// <summary>출처별 수평 이동 배율. 골절 상태 이상 등이 등록·해제한다 (상태가 바뀔 때만 호출).</summary>
        public void SetSpeedMultiplier(string sourceKey, float multiplier) => speedMultipliers[sourceKey] = multiplier;
        public void ClearSpeedMultiplier(string sourceKey) => speedMultipliers.Remove(sourceKey);
        public void SetJumpMultiplier(string sourceKey, float multiplier) => jumpMultipliers[sourceKey] = multiplier;
        public void ClearJumpMultiplier(string sourceKey) => jumpMultipliers.Remove(sourceKey);

        internal void RaiseLanded(float fallDistanceTiles) => Landed?.Invoke(fallDistanceTiles);

        /// <summary>맵 이동으로 플레이어를 다른 곳에 놓을 때 MapLoader가 쓴다. 위치만 바꾸면 Falling
        /// 상태가 이전 위치를 기준으로 낙하 거리를 재서(FallingMotionState.startY), 도착하자마자 엉뚱한
        /// 낙하 피해가 들어갈 수 있다 — 접지 상태로 되돌려 다음 FixedUpdate에서 새 위치 기준으로 다시
        /// 판정하게 한다.</summary>
        public void TeleportTo(Vector2 position)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpCutRequested = false;
            ChangeState(groundedState);
        }

        private static float Multiply(Dictionary<string, float> multipliers)
        {
            float result = 1f;
            foreach (float value in multipliers.Values) result *= value;
            return result;
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
