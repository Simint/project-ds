using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 공격 한 번의 판정 영역. 플레이어 근접 공격과 적 공격이 함께 쓴다 (ARCHITECTURE.md 4.6). 콜라이더는
    /// 트리거로 미리 붙여 두고 Activate/비활성만 토글한다(Instantiate 없이 재사용, 성능 기준 2장).
    /// 대상 레이어는 이 오브젝트 자신의 Layer(PlayerAttack/EnemyAttack)와 물리 Layer Collision
    /// Matrix(RENDERING_RULES.md 3장)가 이미 가른다 — 여기서 다시 마스크를 걸지 않는다. 활성화된 동안
    /// 겹친 IDamageable마다 한 번만 맞는다(같은 스윙에서 여러 번 맞지 않도록).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AttackHitbox : MonoBehaviour
    {
        [SerializeField] private Collider2D hitboxCollider;

        private readonly HashSet<IDamageable> hitThisActivation = new HashSet<IDamageable>();
        // 피해를 받지 않는 트리거(채집물·상자 등)를 같은 스윙 동안 다시 찾지 않도록 기억한다.
        private readonly HashSet<Collider2D> ignoredThisActivation = new HashSet<Collider2D>();
        private DamageInfo pendingInfo;
        private float remainingSeconds;
        private bool active;

        private void Awake()
        {
            if (hitboxCollider == null) Debug.LogError("[AttackHitbox] hitboxCollider 참조가 비어 있습니다.", this);
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }

        private void Update()
        {
            if (!active) return;
            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f) Deactivate();
        }

        /// <summary>activeSeconds 동안 겹친 IDamageable마다 info로 TakeDamage를 한 번씩 부른다.</summary>
        public void Activate(DamageInfo info, float activeSeconds)
        {
            if (hitboxCollider == null) return;
            pendingInfo = info;
            hitThisActivation.Clear();
            ignoredThisActivation.Clear();
            remainingSeconds = activeSeconds;
            active = true;
            hitboxCollider.enabled = true;
        }

        private void Deactivate()
        {
            active = false;
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (!active || ignoredThisActivation.Contains(other)) return;
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                ignoredThisActivation.Add(other);
                return;
            }
            if (hitThisActivation.Contains(target)) return;
            hitThisActivation.Add(target);
            target.TakeDamage(pendingInfo);
        }
    }
}
