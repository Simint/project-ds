using UnityEngine;

namespace Game.Player
{
    /// <summary>땅 위. 걷기/두 발 달리기/네 발 달리기는 같은 수평 이동식이고 프리셋만 다르다.</summary>
    internal sealed class GroundedMotionState : IPlayerMotionState
    {
        public void Enter(PlayerController controller)
        {
        }

        public void Tick(PlayerController controller, float deltaTime)
        {
            Rigidbody2D body = controller.Body;
            MovementPreset preset = controller.ResolvePreset();
            float x = preset.NextHorizontalVelocity(body.linearVelocity.x, controller.HorizontalInput, 1f, deltaTime);
            body.linearVelocity = new Vector2(x, 0f);

            if (!controller.IsGrounded) controller.ChangeState(controller.FallingState);
        }
    }
}
