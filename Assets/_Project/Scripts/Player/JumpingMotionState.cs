using UnityEngine;

namespace Game.Player
{
    /// <summary>점프 상승 구간. 정점(수직 속도 0 이하)에 닿으면 Falling으로 넘어간다.</summary>
    internal sealed class JumpingMotionState : IPlayerMotionState
    {
        public void Enter(PlayerController controller)
        {
        }

        public void Tick(PlayerController controller, float deltaTime)
        {
            Rigidbody2D body = controller.Body;
            PlayerMovementSettings settings = controller.Settings;
            MovementPreset preset = controller.ResolvePreset();

            float x = preset.NextHorizontalVelocity(body.linearVelocity.x, controller.HorizontalInput, settings.AirControl * controller.SpeedMultiplier, deltaTime);
            float y = body.linearVelocity.y - settings.Gravity * deltaTime;
            body.linearVelocity = new Vector2(x, y);

            if (y <= 0f) controller.ChangeState(controller.FallingState);
        }
    }
}
