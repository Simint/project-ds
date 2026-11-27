using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 적 하나의 체력 (ARCHITECTURE.md 4.6). AI(AI/)가 스폰될 때 Initialize로 최대 체력을 정하고,
    /// Died를 구독해 처리한다. Combat은 CreatureDefinition(AI/)을 모른다 — 숫자만 받는다(계층 분리).
    /// 넉백 속도를 Rigidbody2D에 직접 적용하고, IsKnockedBack 동안은 AI가 움직임(velocity.x)을 덮어쓰지
    /// 않아야 한다 — PlayerController.KnockbackMotionState와 같은 이유(ARCHITECTURE.md 4.2/4.7).
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        // 넉백이 다음 AI 틱에 바로 덮어써지지 않도록 잠깐 움직임을 양보하는 시간(기술 값 — 밀려나는
        // 속도 자체는 DamageInfo.KnockbackVelocity 쪽 밸런스 수치다, Pickup.PickupDelaySeconds와 같은 이유).
        private const float KnockbackStunSeconds = 0.15f;

        [SerializeField] private Rigidbody2D body;

        private Health health;
        private float knockbackRemainingSeconds;

        /// <summary>맞을 때마다 (현재, 최대). 체력바 UI가 구독한다.</summary>
        public event Action<float, float> Damaged;
        /// <summary>죽은 개체 자신을 넘긴다 — 구독자가 람다 없이 메서드로 구독·해제할 수 있게.</summary>
        public event Action<EnemyHealth> Died;

        public bool IsDead => health != null && health.IsDead;
        public bool IsKnockedBack => knockbackRemainingSeconds > 0f;

        private void Awake()
        {
            if (body == null) Debug.LogError("[EnemyHealth] body 참조가 비어 있습니다.", this);
        }

        private void Update()
        {
            if (knockbackRemainingSeconds > 0f) knockbackRemainingSeconds -= Time.deltaTime;
        }

        /// <summary>스폰 시 한 번 부른다.</summary>
        public void Initialize(float maxHealth)
        {
            health = new Health(maxHealth);
            knockbackRemainingSeconds = 0f;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (health == null || health.IsDead) return;
            health.Damage(info.Amount);
            Damaged?.Invoke(health.Current, health.Max);

            if (body != null && info.KnockbackVelocity.sqrMagnitude > 0f)
            {
                body.linearVelocity = info.KnockbackVelocity;
                knockbackRemainingSeconds = KnockbackStunSeconds;
            }

            if (health.IsDead) Died?.Invoke(this);
        }
    }
}
