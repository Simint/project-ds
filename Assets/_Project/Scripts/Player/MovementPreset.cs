using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 걷기/달리기 프리셋 하나의 수평 이동 수치. PPU 16 기준으로 1타일 = 1유닛이라 타일/초 값을
    /// 그대로 유닛/초로 쓴다 (RENDERING_RULES.md 1장, tune-movement 스킬).
    /// </summary>
    [Serializable]
    public sealed class MovementPreset
    {
        [SerializeField] private float maxSpeedTiles = 0f;
        [SerializeField] private float accelSeconds = 0f;
        [SerializeField] private float decelSeconds = 0f;

        public float MaxSpeedTiles => maxSpeedTiles;
        public float AccelSeconds => accelSeconds;
        public float DecelSeconds => decelSeconds;

        /// <summary>
        /// 다음 프레임의 수평 속도. 목표 속도보다 느리면 accelSeconds, 더 빠르면(감속) decelSeconds 동안
        /// maxSpeedTiles에 도달하는 비율로 선형 보간한다. controlMultiplier는 공중 제어력(airControl)이다.
        /// </summary>
        public float NextHorizontalVelocity(float currentVelocityX, float moveInputX, float controlMultiplier, float deltaTime)
        {
            float target = moveInputX * maxSpeedTiles * controlMultiplier;
            bool accelerating = Mathf.Abs(target) > Mathf.Abs(currentVelocityX);
            float timeToMax = accelerating ? accelSeconds : decelSeconds;
            float maxDelta = timeToMax > 0f ? (maxSpeedTiles / timeToMax) * deltaTime : Mathf.Abs(target - currentVelocityX);
            return Mathf.MoveTowards(currentVelocityX, target, maxDelta);
        }
    }
}
