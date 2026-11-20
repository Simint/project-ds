namespace Game.Player
{
    /// <summary>
    /// PlayerController의 이동 상태 하나 (State 패턴, .claude/rules/csharp.md "디자인 패턴").
    /// 상태마다 클래스 하나로 나눈다 (ARCHITECTURE.md 4.2: Idle/Run/BeastRun은 Grounded 하나로,
    /// Jump/Fall은 중력 계산이 달라 따로 둔다).
    /// </summary>
    internal interface IPlayerMotionState
    {
        void Enter(PlayerController controller);

        void Tick(PlayerController controller, float deltaTime);
    }
}
