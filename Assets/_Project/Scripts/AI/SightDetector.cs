using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 장애물에 막히지 않은 시야 판정 (ARCHITECTURE.md 4.7). 사거리는 호출하는 쪽이 정한다 — 플레이어가
    /// 앉아 있으면 적 기본 시야 거리에 CombatSettings.CrouchSightMultiplier(<1)를 곱해서 넘긴다
    /// (docs/design/GAME_DESIGN.md 6.5 "몸이 낮아져 적의 시야 거리도 줄어든다").
    /// </summary>
    public static class SightDetector
    {
        public static bool CanSee(Vector2 observerPosition, Vector2 targetPosition, float sightRange, LayerMask obstructionMask)
        {
            if (sightRange <= 0f) return false;
            float sqrDistance = (targetPosition - observerPosition).sqrMagnitude;
            if (sqrDistance > sightRange * sightRange) return false;

            RaycastHit2D hit = Physics2D.Linecast(observerPosition, targetPosition, obstructionMask);
            return hit.collider == null;
        }
    }
}
