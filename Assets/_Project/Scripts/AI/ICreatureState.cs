namespace Game.AI
{
    /// <summary>
    /// CreatureBrain의 행동 상태 하나 (State 패턴, .claude/rules/csharp.md "디자인 패턴").
    /// 상태마다 클래스 하나로 나눈다 (ARCHITECTURE.md 4.7: Idle, Investigate, Chase, Attack — 첫 전투
    /// 버전(6.5)엔 Wander·Flee·Return은 없다).
    /// </summary>
    internal interface ICreatureState
    {
        void Enter(CreatureBrain brain);

        void Tick(CreatureBrain brain, float deltaTime);
    }
}
