using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 이동·점프 수치 (ARCHITECTURE.md 4.2, tune-movement 스킬 시작값). walk는 기본 두 발 걷기,
    /// run은 사람 손+달리기 키, beastRun은 짐승 손+달리기 키(네 발)다. 짐승 손이라도 달리기 키를
    /// 누르지 않으면 walk를 쓴다 (docs/design/GAME_DESIGN.md 3.1).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Player/Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("수평 이동 프리셋")]
        [SerializeField] private MovementPreset walk = new MovementPreset();
        [SerializeField] private MovementPreset run = new MovementPreset();
        [SerializeField] private MovementPreset beastRun = new MovementPreset();

        [Header("점프")]
        [SerializeField] private float airControl = 0.7f;
        [SerializeField] private float jumpHeightTiles = 3.2f;
        [SerializeField] private float timeToApexSeconds = 0.38f;
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float maxFallSpeedTiles = 18f;
        [SerializeField] private float coyoteTimeSeconds = 0.1f;
        [SerializeField] private float jumpBufferSeconds = 0.1f;

        public MovementPreset Walk => walk;
        public MovementPreset Run => run;
        public MovementPreset BeastRun => beastRun;

        public float AirControl => airControl;
        public float JumpHeightTiles => jumpHeightTiles;
        public float TimeToApexSeconds => timeToApexSeconds;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float JumpCutMultiplier => jumpCutMultiplier;
        public float MaxFallSpeedTiles => maxFallSpeedTiles;
        public float CoyoteTimeSeconds => coyoteTimeSeconds;
        public float JumpBufferSeconds => jumpBufferSeconds;

        /// <summary>jumpHeightTiles, timeToApexSeconds에서 계산한 중력 가속도(유닛/초²).</summary>
        public float Gravity => 2f * jumpHeightTiles / (timeToApexSeconds * timeToApexSeconds);

        /// <summary>점프 시작 시 수직 속도(유닛/초).</summary>
        public float JumpVelocity => Gravity * timeToApexSeconds;
    }
}
