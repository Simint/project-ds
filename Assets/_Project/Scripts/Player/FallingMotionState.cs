using UnityEngine;

namespace Game.Player
{
    /// <summary>낙하 구간. 상승보다 중력이 더 세고(fallGravityMultiplier), 낙하 속도는 상한이 있다.</summary>
    internal sealed class FallingMotionState : IPlayerMotionState
    {
        public void Enter(PlayerController controller)
        {
        }

        public void Tick(PlayerController controller, float deltaTime)
        {
            Rigidbody2D body = controller.Body;
            PlayerMovementSettings settings = controller.Settings;
            MovementPreset preset = controller.ResolvePreset();

            float x = preset.NextHorizontalVelocity(body.linearVelocity.x, controller.HorizontalInput, settings.AirControl, deltaTime);
            float y = body.linearVelocity.y - settings.Gravity * settings.FallGravityMultiplier * deltaTime;
            y = Mathf.Max(y, -settings.MaxFallSpeedTiles);
            body.linearVelocity = new Vector2(x, y);

            if (controller.IsGrounded) controller.ChangeState(controller.GroundedState);
        }
    }
}
