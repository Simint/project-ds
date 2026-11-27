using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 맞아서 밀려나는 동안 (ARCHITECTURE.md 4.2 "Stunned", docs/design/GAME_DESIGN.md 6.5 넉백). 입력을
    /// 무시하고 받은 속도가 자연히(공중이면 중력만) 가도록 두다가, 시간이 지나면 땅인지 공중인지에
    /// 따라 Grounded/Falling으로 돌아간다.
    /// </summary>
    internal sealed class KnockbackMotionState : IPlayerMotionState
    {
        private float remainingSeconds;

        public void Enter(PlayerController controller)
        {
            controller.Body.linearVelocity = controller.ConsumePendingKnockbackVelocity();
            remainingSeconds = controller.ConsumePendingStunSeconds();
        }

        public void Tick(PlayerController controller, float deltaTime)
        {
            Rigidbody2D body = controller.Body;
            PlayerMovementSettings settings = controller.Settings;

            float y = controller.IsGrounded ? body.linearVelocity.y : body.linearVelocity.y - settings.Gravity * deltaTime;
            body.linearVelocity = new Vector2(body.linearVelocity.x, y);

            remainingSeconds -= deltaTime;
            if (remainingSeconds > 0f) return;

            controller.ChangeState(controller.IsGrounded ? controller.GroundedState : controller.FallingState);
        }
    }
}
