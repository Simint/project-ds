using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 낙하 구간. 상승보다 중력이 더 세고(fallGravityMultiplier), 낙하 속도는 상한이 있다. 떨어지기
    /// 시작한 높이를 기억해 땅에 닿는 순간 떨어진 거리를 PlayerController.Landed로 알린다
    /// (FallDamage가 구독, docs/design/GAME_DESIGN.md 5.4). 인스턴스 하나를 계속 재사용하므로
    /// Enter마다 시작 높이를 다시 잰다.
    /// </summary>
    internal sealed class FallingMotionState : IPlayerMotionState
    {
        private float startY;

        public void Enter(PlayerController controller)
        {
            startY = controller.Body.position.y;
        }

        public void Tick(PlayerController controller, float deltaTime)
        {
            Rigidbody2D body = controller.Body;
            PlayerMovementSettings settings = controller.Settings;
            MovementPreset preset = controller.ResolvePreset();

            float x = preset.NextHorizontalVelocity(body.linearVelocity.x, controller.HorizontalInput, settings.AirControl * controller.SpeedMultiplier, deltaTime);
            float y = body.linearVelocity.y - settings.Gravity * settings.FallGravityMultiplier * deltaTime;
            y = Mathf.Max(y, -settings.MaxFallSpeedTiles);
            body.linearVelocity = new Vector2(x, y);

            if (controller.IsGrounded)
            {
                controller.RaiseLanded(Mathf.Max(0f, startY - body.position.y));
                controller.ChangeState(controller.GroundedState);
            }
        }
    }
}
