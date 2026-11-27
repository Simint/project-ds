using Game.Combat;
using Game.Core;
using Game.Inventory;
using Game.Survival;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 부술 수 있는 벽 (막힌 통로, 판자 바리케이드 등 — docs/design/GAME_DESIGN.md 4.3의 7단계 범위,
    /// ARCHITECTURE.md 3.4). 플레이어 공격(PlayerMeleeAttack → AttackHitbox)으로 여러 번 쳐서 부순다.
    /// - 칠 때마다 소음이 나고, 손에 든 도구(내구도가 있는 것)의 내구도가 준다.
    /// - 날붙이로는 깎이지 않는다(소음만 난다).
    /// - 짐승 손 발톱은 배율만큼 덜 깎여 훨씬 오래 걸리고, 칠 때마다 플레이어 HP가 줄며(1에서 멈춤) 확률로 골절·출혈이 생긴다.
    /// 부순 벽은 영구히 남는다(4.2). State: 0=멀쩡, 1=부서짐. 남은 내구는 저장하지 않는다 — 맵을 나갔다
    /// 오면 처음부터 다시 쳐야 한다(부서진 것만 영구, 4.2 표).
    /// 막는 콜라이더는 Ground 레이어, 타격 판정은 Interactable 레이어 트리거(PlayerAttack과 닿음)가 맡는다.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class BreakableWall : MonoBehaviour, IDamageable, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private BreakableWallDefinition definition;
        [SerializeField] private ExplorationSettings settings;
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private Collider2D hitTrigger;
        [SerializeField] private SpriteRenderer visual;

        private Health durability;
        private bool broken;

        public string Id => persistentId != null ? persistentId.Id : "";
        public bool IsBroken => broken;
        public float RemainingDurability => durability != null ? durability.Current : 0f;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[BreakableWall] persistentId 참조가 비어 있습니다.", this);
            if (definition == null) Debug.LogError("[BreakableWall] definition 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[BreakableWall] settings 참조가 비어 있습니다.", this);
            if (blockingCollider == null) Debug.LogError("[BreakableWall] blockingCollider 참조가 비어 있습니다.", this);
            if (hitTrigger == null) Debug.LogError("[BreakableWall] hitTrigger 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[BreakableWall] visual 참조가 비어 있습니다.", this);
            ResetDurability();
            RefreshVisual();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (broken || durability == null || settings == null) return;
            PlayerHeldItem held = info.Attacker != null ? info.Attacker.GetComponentInParent<PlayerHeldItem>() : null;
            // 내구도가 다 닳은 도구·무기로는 벽이 깎이지 않는다(7장 "내구도 0이면 쓸 수 없다", 쇠지렛대와 같은 기준).
            if (info.Kind != DamageKind.Claw && held != null && held.SelectedSlot != null && held.SelectedSlot.Durability == 0) return;

            if (settings.WallHitNoiseRadiusTiles > 0f)
                GameServices.NoiseSystem?.Emit(transform.position, settings.WallHitNoiseRadiusTiles, NoiseKind.WallHit);

            if (!BreakableWallRules.CanDamageWall(info.Kind)) return;
            if (info.Kind == DamageKind.Claw) HurtClawingPlayer(info.Attacker);
            else WearHeldTool(held);

            durability.Damage(BreakableWallRules.DamageToWall(info, settings.ClawWallDamageMultiplier));
            if (durability.IsDead) SetBroken(true);
        }

        /// <summary>맨손(발톱)으로 벽을 치면 손이 다친다(4.3 7단계 범위).</summary>
        private void HurtClawingPlayer(GameObject attacker)
        {
            if (attacker == null) return;
            PlayerDamageReceiver self = attacker.GetComponentInParent<PlayerDamageReceiver>();
            PlayerSurvival survival = attacker.GetComponentInParent<PlayerSurvival>();
            SurvivalStat hp = survival != null ? survival.Stats.Get(StatIds.Hp) : null;
            if (self != null && hp != null)
            {
                float amount = BreakableWallRules.ClawSelfDamage(hp.Value, settings.ClawWallHpCost);
                self.TakeDamage(new DamageInfo(amount, DamageKind.Claw, settings.ClawWallBleedChance, Vector2.zero, gameObject));
            }

            if (settings.ClawWallFractureChance > 0f && Random.value < settings.ClawWallFractureChance)
                attacker.GetComponentInParent<PlayerStatusEffects>()?.Apply(StatusEffectIds.Fracture);
        }

        private void WearHeldTool(PlayerHeldItem held)
        {
            if (settings.WallToolDurabilityCost <= 0 || held == null || held.Inventory == null) return;
            held.Inventory.Model.DamageTool(held.SelectedSlotIndex, settings.WallToolDurabilityCost);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(broken ? 1 : 0, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            ResetDurability();
            SetBroken(record.State == 1);
        }

        private void ResetDurability() => durability = new Health(definition != null ? definition.MaxDurability : 0f);

        private void SetBroken(bool value)
        {
            broken = value;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (blockingCollider != null) blockingCollider.enabled = !broken;
            if (hitTrigger != null) hitTrigger.enabled = !broken;
            if (visual != null) visual.enabled = !broken;
        }
    }
}
