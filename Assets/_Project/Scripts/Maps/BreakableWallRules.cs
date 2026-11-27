using Game.Combat;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 벽 타격 계산 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위). 둔기·도구는 피해 그대로, 짐승 손 발톱은 배율을
    /// 곱해 훨씬 많이 쳐야 하고, 날붙이로는 벽이 깎이지 않는다(소음만). 순수 함수라 EditMode 테스트로 검증한다.
    /// </summary>
    public static class BreakableWallRules
    {
        public static bool CanDamageWall(DamageKind kind) => kind != DamageKind.Blade;

        public static float DamageToWall(DamageInfo info, float clawMultiplier)
        {
            if (info.Amount <= 0f || !CanDamageWall(info.Kind)) return 0f;
            return info.Kind == DamageKind.Claw ? info.Amount * clawMultiplier : info.Amount;
        }

        /// <summary>발톱으로 벽을 칠 때 플레이어가 잃는 HP. 자해로는 죽지 않도록 HP 1에서 멈춘다(출혈은 따로 계속된다).</summary>
        public static float ClawSelfDamage(float currentHp, float cost)
        {
            if (cost <= 0f) return 0f;
            return Mathf.Max(0f, Mathf.Min(cost, currentHp - 1f));
        }
    }
}
