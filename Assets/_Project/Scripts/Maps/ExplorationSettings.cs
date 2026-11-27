using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 탐색 요소(잠긴 문·부술 수 있는 벽·땅 파기)가 함께 쓰는 수치 (docs/design/GAME_DESIGN.md 4.3의 7단계
    /// 범위). 밸런스 수치라 실제 값은 비공개 Data/ 에셋에만 둔다 — 여기 기본값은 0이나 중립 값이다
    /// (.claude/rules/csharp.md "수치와 이름").
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Maps/Exploration Settings")]
    public sealed class ExplorationSettings : ScriptableObject
    {
        [Header("잠긴 문 — 쇠지렛대")]
        [SerializeField] [Min(0f)] private float crowbarHoldSeconds = 0f;
        [SerializeField] [Min(0f)] private float crowbarNoiseRadiusTiles = 0f;
        [SerializeField] [Min(0)] private int crowbarDurabilityCost = 0;

        [Header("잠긴 문 — 무기로 치기 (흔들리기만 하고 큰 소음)")]
        [SerializeField] [Min(0f)] private float doorBashNoiseRadiusTiles = 0f;
        [Tooltip("흔들리는 연출의 길이(초)와 폭(타일). 표시용이다.")]
        [SerializeField] [Min(0f)] private float doorShakeSeconds = 0f;
        [SerializeField] [Min(0f)] private float doorShakeAmplitudeTiles = 0f;

        [Header("부술 수 있는 벽")]
        [SerializeField] [Min(0f)] private float wallHitNoiseRadiusTiles = 0f;
        [Tooltip("벽을 칠 때마다 손에 든 도구(내구도가 있는 것)에서 깎이는 내구도.")]
        [SerializeField] [Min(0)] private int wallToolDurabilityCost = 0;
        [Tooltip("발톱 피해에 곱하는 배율(1보다 작을수록 더 많이 쳐야 한다).")]
        [SerializeField] [Min(0f)] private float clawWallDamageMultiplier = 1f;
        [Tooltip("발톱으로 칠 때마다 플레이어가 잃는 HP.")]
        [SerializeField] [Min(0f)] private float clawWallHpCost = 0f;
        [SerializeField] [Range(0f, 1f)] private float clawWallFractureChance = 0f;
        [SerializeField] [Range(0f, 1f)] private float clawWallBleedChance = 0f;

        [Header("땅 파기")]
        [SerializeField] [Min(0f)] private float digHoldSeconds = 0f;

        public float CrowbarHoldSeconds => crowbarHoldSeconds;
        public float CrowbarNoiseRadiusTiles => crowbarNoiseRadiusTiles;
        public int CrowbarDurabilityCost => crowbarDurabilityCost;
        public float DoorBashNoiseRadiusTiles => doorBashNoiseRadiusTiles;
        public float DoorShakeSeconds => doorShakeSeconds;
        public float DoorShakeAmplitudeTiles => doorShakeAmplitudeTiles;
        public float WallHitNoiseRadiusTiles => wallHitNoiseRadiusTiles;
        public int WallToolDurabilityCost => wallToolDurabilityCost;
        public float ClawWallDamageMultiplier => clawWallDamageMultiplier;
        public float ClawWallHpCost => clawWallHpCost;
        public float ClawWallFractureChance => clawWallFractureChance;
        public float ClawWallBleedChance => clawWallBleedChance;
        public float DigHoldSeconds => digHoldSeconds;
    }
}
