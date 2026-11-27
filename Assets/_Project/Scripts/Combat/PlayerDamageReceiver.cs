using Game.Player;
using Game.Survival;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 플레이어가 맞는 쪽(적 공격의 IDamageable, ARCHITECTURE.md 4.6). HP는 SurvivalStats로, 넉백은
    /// PlayerController.ApplyKnockback으로 넘긴다. 출혈 확률은 공격 쪽(적)이 DamageInfo.BleedChance로
    /// 정한다 — 좀비·들개 모두 공격이 맞으면 확률로 출혈이 생긴다(docs/design/GAME_DESIGN.md 6.5).
    /// </summary>
    public sealed class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        // 적에게 맞고 잠깐 움직임을 빼앗기는 시간(기술 값). 밀려나는 속도 자체는
        // DamageInfo.KnockbackVelocity 쪽 밸런스 수치다 (Pickup.PickupDelaySeconds와 같은 이유).
        private const float StunSeconds = 0.2f;

        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerStatusEffects statusEffects;
        [SerializeField] private PlayerController controller;

        private void Awake()
        {
            if (survival == null) Debug.LogError("[PlayerDamageReceiver] survival 참조가 비어 있습니다.", this);
            if (statusEffects == null) Debug.LogError("[PlayerDamageReceiver] statusEffects 참조가 비어 있습니다.", this);
            if (controller == null) Debug.LogError("[PlayerDamageReceiver] controller 참조가 비어 있습니다.", this);
        }

        public void TakeDamage(DamageInfo info)
        {
            SurvivalStat hp = survival != null ? survival.Stats.Get(StatIds.Hp) : null;
            if (hp == null) return;

            hp.Add(-info.Amount);
            survival.Stats.NotifyChanged();
            survival.CheckDeath();

            if (info.BleedChance > 0f && Random.value < info.BleedChance)
                statusEffects?.Apply(StatusEffectIds.Bleeding);

            if (controller != null && info.KnockbackVelocity.sqrMagnitude > 0f)
                controller.ApplyKnockback(info.KnockbackVelocity, StunSeconds);
        }
    }
}
