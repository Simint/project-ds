namespace Game.Building
{
    /// <summary>빗물 통이 차는 계산 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 순수 함수라 EditMode 테스트로 검증한다.</summary>
    public static class RainBarrelRules
    {
        /// <summary>비가 minutes분 동안 왔을 때 (차 있는 물병 수, 1병이 되기 전 남은 양). 가득 차면 더 늘지 않는다.</summary>
        public static (int stored, float partial) Fill(int stored, float partial, int capacity, float bottlesPerHour, long minutes)
        {
            if (capacity <= 0 || bottlesPerHour <= 0f || minutes <= 0) return (System.Math.Max(0, stored), partial);
            if (stored >= capacity) return (capacity, 0f);

            double total = partial + bottlesPerHour * (minutes / 60.0);
            int whole = (int)System.Math.Floor(total);
            int next = stored + whole;
            if (next >= capacity) return (capacity, 0f);
            return (next, (float)(total - whole));
        }
    }
}
