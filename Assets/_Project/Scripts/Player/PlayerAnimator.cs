using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 표시 전용. PlayerController의 이동 상태(접지 여부, 수평 속도, 달리기 프리셋, 수직 속도)를 읽어
    /// Animator 파라미터만 갱신한다. 이동 로직은 이 컴포넌트를 모른다 — 지워도 조작은 그대로다.
    /// 상태·전이·재생 속도는 Animator Controller(Art 서브모듈)에 둔다.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int RunningHash = Animator.StringToHash("Running");
        private static readonly int MoveSpeedScaleHash = Animator.StringToHash("MoveSpeedScale");

        [SerializeField] private PlayerController controller;
        [SerializeField] private Animator animator;

        [Tooltip("걷기·달리기 클립 재생 속도 배율의 범위. 실제 속도 ÷ 그 프리셋 최고 속도에 곱해지는 값을 이 범위로 자른다.")]
        [SerializeField] private Vector2 moveSpeedScaleRange = new Vector2(0.5f, 2f);

        private void Awake()
        {
            if (controller == null) Debug.LogError("[PlayerAnimator] controller 참조가 비어 있습니다.", this);
            if (animator == null) Debug.LogError("[PlayerAnimator] animator 참조가 비어 있습니다.", this);
        }

        private void Update()
        {
            if (controller == null || animator == null || controller.Body == null || controller.Settings == null) return;

            Vector2 velocity = controller.Body.linearVelocity;
            float speed = Mathf.Abs(velocity.x);
            PlayerMovementSettings settings = controller.Settings;
            MovementPreset preset = controller.ResolvePreset();
            bool running = preset == settings.Run || preset == settings.BeastRun;

            // 같은 걷기/달리기 클립을 프리셋마다 발이 미끄러지지 않게 재생 속도로 맞춘다. 기준은 각 클립이
            // 그려진 프리셋(걷기 클립 = Walk, 달리기 클립 = Run)의 최고 속도다. 네 발 달리기(더 빠름)와
            // 앉아 걷기(더 느림)는 그림이 생기기 전까지 이 배율로 빨라지거나 느려진다.
            float reference = running ? settings.Run.MaxSpeedTiles : settings.Walk.MaxSpeedTiles;
            float scale = reference > 0f ? speed / reference : 1f;
            scale = Mathf.Clamp(scale, moveSpeedScaleRange.x, moveSpeedScaleRange.y);

            animator.SetFloat(SpeedHash, speed);
            animator.SetFloat(VerticalSpeedHash, velocity.y);
            animator.SetBool(GroundedHash, controller.IsInGroundedState);
            animator.SetBool(RunningHash, running);
            animator.SetFloat(MoveSpeedScaleHash, scale);
        }
    }
}
