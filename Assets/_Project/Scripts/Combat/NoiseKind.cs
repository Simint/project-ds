namespace Game.Combat
{
    /// <summary>소음 종류 (ARCHITECTURE.md 4.6). 공격·총성·문 부수기 등은 그 시스템이 생기는 단계에서
    /// 추가한다 (docs/design/GAME_DESIGN.md 6.2).</summary>
    public enum NoiseKind
    {
        Footstep,
        /// <summary>잠긴 문을 무기로 쳐서 흔들 때 (4.3 7단계: 큰 소음).</summary>
        DoorBash,
        /// <summary>쇠지렛대로 문을 열 때 (작은 소음).</summary>
        Crowbar,
        /// <summary>부술 수 있는 벽을 칠 때.</summary>
        WallHit
    }
}
