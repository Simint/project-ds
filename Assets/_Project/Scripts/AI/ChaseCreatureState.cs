using UnityEngine;

namespace Game.AI
{
    /// <summary>알아챈 플레이어를 쫓는다. 한 번 알아채면 사거리에 들어올 때까지 계속 쫓는다 — 다른 맵으로
    /// 넘어가면 추격이 끊기는 건 씬이 바뀌어 이 오브젝트 자체가 사라지기 때문이다(ARCHITECTURE.md 3.4
    /// "적 개체 하나하나는 저장하지 않는다. 맵을 떠나면 사라진다", docs/design/GAME_DESIGN.md 6.5).</summary>
    internal sealed class ChaseCreatureState : ICreatureState
    {
        public void Enter(CreatureBrain brain)
        {
        }

        public void Tick(CreatureBrain brain, float deltaTime)
        {
            if (!brain.TryGetPlayerPosition(out Vector2 playerPosition)) { brain.ChangeState(brain.IdleState); return; }

            float distance = Vector2.Distance(brain.Body.position, playerPosition);
            if (distance <= brain.Definition.AttackRangeTiles) { brain.ChangeState(brain.AttackState); return; }

            brain.MoveToward(playerPosition);
        }
    }
}
