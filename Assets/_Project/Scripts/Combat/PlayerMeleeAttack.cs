using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using Game.Survival;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 좌클릭/패드 공격 버튼으로 바라보는 방향 앞쪽을 휘두른다 (docs/design/GAME_DESIGN.md 6.5). 짐승
    /// 손이면 항상 발톱(손에 든 것과 무관), 사람 손이면 단축칸에 든 무기(ItemDefinition.Weapon)만
    /// 쓴다 — 사람 손의 맨손 전투는 없다(3.1 "할 수 없는 것: 발톱 공격"). 스태미나가 모자라면
    /// 휘두르지 않는다(6.5 "스태미나가 바닥나면 공격이 막힌다").
    /// </summary>
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        // 공격 판정이 열려 있는 시간(기술 값, 스윙 애니메이션이 없어 임의로 짧게 둔다. 무기별 쿨타임은
        // WeaponData.AttackCooldownSeconds 쪽 밸런스 수치다).
        private const float ActiveSeconds = 0.1f;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private PlayerHeldItem heldItem;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private CombatSettings settings;
        [SerializeField] private AttackHitbox hitbox;
        [SerializeField] private Transform hitboxAnchor;

        private float cooldownRemaining;

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[PlayerMeleeAttack] inputReader 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerMeleeAttack] hands 참조가 비어 있습니다.", this);
            if (facing == null) Debug.LogError("[PlayerMeleeAttack] facing 참조가 비어 있습니다.", this);
            if (heldItem == null) Debug.LogError("[PlayerMeleeAttack] heldItem 참조가 비어 있습니다.", this);
            if (survival == null) Debug.LogError("[PlayerMeleeAttack] survival 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerMeleeAttack] settings 참조가 비어 있습니다.", this);
            if (hitbox == null) Debug.LogError("[PlayerMeleeAttack] hitbox 참조가 비어 있습니다.", this);
            if (hitboxAnchor == null) Debug.LogError("[PlayerMeleeAttack] hitboxAnchor 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.AttackPressed += HandleAttackPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.AttackPressed -= HandleAttackPressed;
        }

        private void Update()
        {
            if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
        }

        private void HandleAttackPressed()
        {
            // 건설 모드에서는 왼쪽 클릭이 설치로 쓰인다 (BuildPlace와 같은 버튼, ItemUseController의
            // BuildDemolish/UseItem 공유와 같은 이유).
            if (GameServices.BuildModeController != null && GameServices.BuildModeController.IsActive) return;
            if (cooldownRemaining > 0f || hands == null || hands.IsTransitioning) return;
            if (survival == null || settings == null || hitbox == null || hitboxAnchor == null || facing == null) return;

            bool beast = hands.Mode == HandMode.Beast;
            float damage, cooldown, staminaCost, range, knockback;
            DamageKind kind;

            if (beast)
            {
                damage = settings.BeastClawDamage;
                cooldown = settings.BeastClawCooldownSeconds;
                staminaCost = settings.BeastClawStaminaCost;
                range = settings.BeastClawRangeTiles;
                knockback = settings.BeastClawKnockbackForce;
                kind = DamageKind.Claw;
            }
            else
            {
                ItemDefinition weapon = heldItem != null ? heldItem.SelectedDefinition : null;
                if (weapon == null || !weapon.IsWeapon) return; // 사람 손은 무기 없이 못 때린다 (3.1)
                // 내구도가 다 닳은 도구는 무기로도 못 쓴다 (7장 "내구도 0이면 쓸 수 없다").
                if (heldItem.SelectedSlot != null && heldItem.SelectedSlot.Durability == 0) return;
                WeaponData data = weapon.Weapon;
                damage = data.Damage;
                cooldown = data.AttackCooldownSeconds;
                staminaCost = data.StaminaCost;
                range = data.RangeTiles;
                knockback = data.KnockbackForce;
                kind = weapon.IsTool ? DamageKind.Tool : data.Kind == WeaponKind.Blade ? DamageKind.Blade : DamageKind.Blunt;
            }

            SurvivalStat stamina = survival.Stats.Get(StatIds.Stamina);
            if (stamina != null && stamina.Value < staminaCost) return; // 모자라면 막힌다 (6.5)

            stamina?.Add(-staminaCost);
            survival.Stats.NotifyChanged();
            cooldownRemaining = cooldown;

            int dir = facing.Direction;
            hitboxAnchor.localPosition = new Vector3(dir * range, hitboxAnchor.localPosition.y, 0f);
            Vector2 knockbackVelocity = new Vector2(dir * knockback, 0f);
            var info = new DamageInfo(damage, kind, 0f, knockbackVelocity, gameObject);
            hitbox.Activate(info, ActiveSeconds);
        }
    }
}
