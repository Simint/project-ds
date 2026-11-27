using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 체력 하나의 상태 (ARCHITECTURE.md 4.6). MonoBehaviour 없이 테스트할 수 있는 순수 C# 클래스다.
    /// EnemyHealth가 감싸서 쓴다. 플레이어 HP는 SurvivalStats(Survival/)가 따로 맡아 이 클래스를 쓰지
    /// 않는다 — 생존 스탯 하나로 통일하기 위해서다.
    /// </summary>
    public sealed class Health
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>실제로 깎인 양과 함께, 맞을 때마다.</summary>
        public event Action<float> Damaged;
        public event Action Died;

        public Health(float max)
        {
            Max = Mathf.Max(0f, max);
            Current = Max;
        }

        public void Damage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            float before = Current;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(before - Current);
            if (IsDead) Died?.Invoke();
        }
    }
}
