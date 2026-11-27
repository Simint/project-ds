namespace Game.AI
{
    /// <summary>가만히 서서 시야에 플레이어가 들어오면 바로 쫓는다. 소음은 Tick이 아니라
    /// CreatureBrain.OnNoiseHeard(이벤트)로 Investigate로 넘어간다.</summary>
    internal sealed class IdleCreatureState : ICreatureState
    {
        public void Enter(CreatureBrain brain) => brain.Stop();

        public void Tick(CreatureBrain brain, float deltaTime)
        {
            if (brain.CanSeePlayer()) brain.ChangeState(brain.ChaseState);
        }
    }
}
