using UnityEngine;

namespace Game.Combat
{
    /// <summary>달릴 때 나는 소음 수치 (docs/design/GAME_DESIGN.md 6.2, ARCHITECTURE.md 4.6). 전부
    /// 임시값으로 시작해 플레이하며 조절한다 (.claude/rules/csharp.md "수치와 이름").</summary>
    [CreateAssetMenu(menuName = "Game/Combat/Noise Settings")]
    public sealed class NoiseSettings : ScriptableObject
    {
        [SerializeField] private float runNoiseRadiusTiles = 0f;
        [SerializeField] private float beastRunNoiseRadiusTiles = 0f;
        [SerializeField] private float footstepIntervalSeconds = 0f;

        public float RunNoiseRadiusTiles => runNoiseRadiusTiles;
        public float BeastRunNoiseRadiusTiles => beastRunNoiseRadiusTiles;
        public float FootstepIntervalSeconds => footstepIntervalSeconds;
    }
}
