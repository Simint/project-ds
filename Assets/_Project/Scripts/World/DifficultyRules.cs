using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 난이도 배율 적용 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 난이도가 없으면(null) 배율 1로 본다. 순수 함수라
    /// EditMode 테스트로 검증한다.
    /// </summary>
    public static class DifficultyRules
    {
        public static float EnemyHealth(float baseHealth, DifficultyDefinition difficulty) =>
            baseHealth * (difficulty != null ? difficulty.EnemyHealthMultiplier : 1f);

        public static float EnemyDamage(float baseDamage, DifficultyDefinition difficulty) =>
            baseDamage * (difficulty != null ? difficulty.EnemyDamageMultiplier : 1f);

        /// <summary>보상 개수. 원래 1개 이상이면 배율을 곱해도 최소 1개는 준다.</summary>
        public static int LootCount(int baseCount, DifficultyDefinition difficulty)
        {
            if (baseCount <= 0) return 0;
            float multiplier = difficulty != null ? difficulty.LootMultiplier : 1f;
            // 은행가 반올림(Mathf.RoundToInt)이 아니라 0.5에서 늘 올린다.
            return Mathf.Max(1, Mathf.FloorToInt(baseCount * multiplier + 0.5f));
        }

        /// <summary>리스폰까지 걸리는 게임 분. 0일이면 리스폰하지 않으므로 0.</summary>
        public static long RespawnMinutes(int respawnDays, DifficultyDefinition difficulty)
        {
            if (respawnDays <= 0) return 0;
            float multiplier = difficulty != null ? difficulty.RespawnTimeMultiplier : 1f;
            return System.Math.Max(1L, (long)Mathf.Round(respawnDays * WorldClock.MinutesPerDay * multiplier));
        }

        public static float SurvivalDecay(DifficultyDefinition difficulty) =>
            difficulty != null ? difficulty.SurvivalDecayMultiplier : 1f;

        public static DeathDropScope DropScope(DifficultyDefinition difficulty) =>
            difficulty != null ? difficulty.DeathDropScope : DeathDropScope.Bag;
    }
}
