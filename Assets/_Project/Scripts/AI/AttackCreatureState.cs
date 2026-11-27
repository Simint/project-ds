using UnityEngine;

namespace Game.AI
{
    /// <summary>사거리 안에서 쿨타임마다 플레이어를 때린다. 플레이어가 사거리를 벗어나면 다시 쫓는다.</summary>
    internal sealed class AttackCreatureState : ICreatureState
    {
        public void Enter(CreatureBrain brain) => brain.Stop();

        public void Tick(CreatureBrain brain, float deltaTime)
        {
            if (!brain.TryGetPlayerPosition(out Vector2 playerPosition)) { brain.ChangeState(brain.IdleState); return; }

            float distance = Vector2.Distance(brain.Body.position, playerPosition);
            if (distance > brain.Definition.AttackRangeTiles) { brain.ChangeState(brain.ChaseState); return; }

            brain.TryAttackPlayer();
        }
    }
}
