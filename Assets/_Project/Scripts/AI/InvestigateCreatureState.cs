using UnityEngine;

namespace Game.AI
{
    /// <summary>소음이 들린 지점으로 다가간다 (docs/design/GAME_DESIGN.md 6.5 "뒤쪽이나 먼 곳에서는
    /// 소음을 들어야 다가온다"). 가다가 플레이어가 보이면 바로 추격, 지점에 도착했는데도 안 보이면
    /// 다시 Idle로 돌아간다.</summary>
    internal sealed class InvestigateCreatureState : ICreatureState
    {
        // 목표 지점에 "도착"으로 치는 거리(기술 값 — 소음 반경 자체는 NoiseSettings 쪽 밸런스 수치다).
        private const float ArrivalThresholdTiles = 0.3f;

        public void Enter(CreatureBrain brain)
        {
        }

        public void Tick(CreatureBrain brain, float deltaTime)
        {
            if (brain.CanSeePlayer()) { brain.ChangeState(brain.ChaseState); return; }

            Vector2 target = brain.InvestigateTarget;
            brain.MoveToward(target);
            // 수평으로만 움직이므로(MoveToward) 도착 판정도 x만 본다 — 소음이 들린 y는 보통 발밑과
            // 다를 수 있어(단차) 그걸로 "도착"을 막으면 제자리에서 영영 못 벗어난다.
            if (Mathf.Abs(brain.Body.position.x - target.x) <= ArrivalThresholdTiles)
                brain.ChangeState(brain.IdleState);
        }
    }
}
