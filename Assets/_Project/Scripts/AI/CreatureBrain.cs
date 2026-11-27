using Game.Combat;
using Game.Core;
using Game.Player;
using Game.World;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 좀비·동물 행동 (ARCHITECTURE.md 4.7, docs/design/GAME_DESIGN.md 6.5). 상태 머신은 State 패턴
    /// (Idle/Investigate/Chase/Attack, .claude/rules/csharp.md). 플레이어 위치·손 모드·앉기 여부는
    /// GameServices로만 접근한다 — Find*/Camera.main을 쓰지 않는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CreatureBrain : MonoBehaviour, INoiseListener
    {
        // 공격 판정이 열려 있는 시간(기술 값, PlayerMeleeAttack.ActiveSeconds와 같은 이유).
        private const float AttackActiveSeconds = 0.1f;

        [SerializeField] private CreatureDefinition definition;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private AttackHitbox attackHitbox;
        [SerializeField] private Transform attackHitboxAnchor;
        [SerializeField] private CombatSettings combatSettings;

        private readonly ICreatureState idleState = new IdleCreatureState();
        private readonly ICreatureState investigateState = new InvestigateCreatureState();
        private readonly ICreatureState chaseState = new ChaseCreatureState();
        private readonly ICreatureState attackState = new AttackCreatureState();

        private ICreatureState currentState;
        private PlayerHands playerHands;
        private float attackCooldownRemaining;

        public CreatureDefinition Definition => definition;
        public Rigidbody2D Body => body;
        public Vector2 InvestigateTarget { get; set; }

        internal ICreatureState IdleState => idleState;
        internal ICreatureState InvestigateState => investigateState;
        internal ICreatureState ChaseState => chaseState;
        internal ICreatureState AttackState => attackState;

        private void Awake()
        {
            if (definition == null) Debug.LogError("[CreatureBrain] definition 참조가 비어 있습니다.", this);
            if (body == null) Debug.LogError("[CreatureBrain] body 참조가 비어 있습니다.", this);
            if (health == null) Debug.LogError("[CreatureBrain] health 참조가 비어 있습니다.", this);
            if (attackHitbox == null) Debug.LogError("[CreatureBrain] attackHitbox 참조가 비어 있습니다.", this);
            if (attackHitboxAnchor == null) Debug.LogError("[CreatureBrain] attackHitboxAnchor 참조가 비어 있습니다.", this);
            if (combatSettings == null) Debug.LogError("[CreatureBrain] combatSettings 참조가 비어 있습니다.", this);
        }

        private void Start()
        {
            if (health != null) health.Initialize(CreatureStatRules.MaxHealth(definition, GameServices.Difficulty));
            playerHands = GameServices.PlayerInventory != null ? GameServices.PlayerInventory.GetComponent<PlayerHands>() : null;
            ChangeState(idleState);
        }

        private void OnEnable() => GameServices.NoiseSystem?.Register(this);

        private void OnDisable() => GameServices.NoiseSystem?.Unregister(this);

        private void Update()
        {
            if (attackCooldownRemaining > 0f) attackCooldownRemaining -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (definition == null || body == null || health == null) return;
            if (health.IsDead || health.IsKnockedBack) return;
            currentState?.Tick(this, Time.fixedDeltaTime);
        }

        private static bool IsNight => GameServices.DayNight != null && GameServices.DayNight.IsNight;

        internal void ChangeState(ICreatureState next)
        {
            currentState = next;
            currentState.Enter(this);
        }

        // 수평으로만 움직이게 하는 거리 임계값(기술 값). 목표와 이 거리 안이면 더 다가가지 않는다.
        private const float StopDistanceTiles = 0.05f;

        /// <summary>횡스크롤이라 수평 속도만 정한다. 수직은 Rigidbody2D의 중력(Gravity Scale)과 땅
        /// 충돌이 그대로 맡는다 — 점프가 없는 지상 적이라 PlayerController 같은 상태 머신 중력 계산이
        /// 필요 없다 (ARCHITECTURE.md 2장 원칙 6).</summary>
        internal void Stop()
        {
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        internal void MoveToward(Vector2 targetPosition)
        {
            if (body == null || definition == null) return;
            float dx = targetPosition.x - body.position.x;
            float moveX = Mathf.Abs(dx) < StopDistanceTiles ? 0f : Mathf.Sign(dx) * CreatureStatRules.MoveSpeed(definition, IsNight);
            body.linearVelocity = new Vector2(moveX, body.linearVelocity.y);
        }

        /// <summary>장애물에 막히지 않고 시야(앉기·짐승 손 보정 포함) 안에 플레이어가 보이는지.</summary>
        internal bool CanSeePlayer()
        {
            if (!TryGetPlayerPosition(out Vector2 playerPosition) || definition == null || body == null) return false;

            float range = CreatureStatRules.SightRange(definition, IsNight, GameServices.WeatherEnemySightMultiplier);
            if (playerHands != null && playerHands.Mode == HandMode.Beast) range *= definition.BeastHandSightMultiplier;
            if (combatSettings != null && GameServices.PlayerInputReader != null && GameServices.PlayerInputReader.CrouchHeld)
                range *= combatSettings.CrouchSightMultiplier;

            return SightDetector.CanSee(body.position, playerPosition, range, GameLayers.SolidMask);
        }

        /// <summary>시야·사거리와 무관하게 플레이어가 지금 어디 있는지만. 이미 쫓기 시작한 뒤(Chase/Attack)
        /// 쓴다 — 거기선 "보이는지"가 아니라 "아직 있는지"만 필요하다.</summary>
        internal bool TryGetPlayerPosition(out Vector2 position)
        {
            if (GameServices.PlayerInventory == null) { position = default; return false; }
            position = GameServices.PlayerInventory.transform.position;
            return true;
        }

        internal void TryAttackPlayer()
        {
            if (definition == null || attackHitbox == null || attackHitboxAnchor == null || body == null) return;
            if (attackCooldownRemaining > 0f) return;
            if (!TryGetPlayerPosition(out Vector2 playerPosition)) return;

            attackCooldownRemaining = definition.AttackCooldownSeconds;

            float dir = Mathf.Sign(playerPosition.x - body.position.x);
            if (dir == 0f) dir = 1f;
            attackHitboxAnchor.localPosition = new Vector3(dir * definition.AttackRangeTiles, attackHitboxAnchor.localPosition.y, 0f);
            Vector2 knockback = new Vector2(dir * definition.AttackKnockbackForce, 0f);
            var info = new DamageInfo(CreatureStatRules.AttackDamage(definition, IsNight, GameServices.Difficulty), DamageKind.Claw, definition.AttackBleedChance, knockback, gameObject);
            attackHitbox.Activate(info, AttackActiveSeconds);
        }

        /// <summary>이미 알아챈 상태(Idle이 아님)면 무시한다 — 쫓거나 때리는 중에 다시 소음으로 끼어들
        /// 필요가 없다(docs/design/GAME_DESIGN.md 6.5).</summary>
        public void OnNoiseHeard(Vector2 position, float radius, NoiseKind kind)
        {
            if (currentState != idleState || body == null) return;
            if (Vector2.Distance(body.position, position) > radius) return;

            InvestigateTarget = position;
            ChangeState(investigateState);
        }
    }
}
