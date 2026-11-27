using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.World;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 좀비·들개를 생성한다 (ARCHITECTURE.md 3.4, 4.7, add-harvestable과 같은 리스폰 방식로
    /// docs/design/GAME_DESIGN.md 6.5). 생성·제거를 이 한 곳(Factory, .claude/rules/csharp.md)에서만
    /// 한다. 적 개체 자체는 저장하지 않는다 — 맵을 떠나면(씬 언로드) 전부 사라지고, "비었는지/다음
    /// 리스폰 시각"만 저장한다(Harvestable과 같은 패턴).
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class EnemySpawner : MonoBehaviour, IPersistentObject
    {
        [SerializeField] private PersistentId persistentId;
        [SerializeField] private CreatureBrain prefab;
        [Tooltip("몇 마리를 어느 범위에 내보낼지. 밸런스 수치라 Data/ 에셋에 둔다.")]
        [SerializeField] private SpawnGroupDefinition spawnGroup;

        private readonly List<CreatureBrain> alive = new List<CreatureBrain>();
        private bool depleted;
        private long respawnAtMinute;

        public string Id => persistentId != null ? persistentId.Id : "";

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[EnemySpawner] persistentId 참조가 비어 있습니다.", this);
            if (prefab == null) Debug.LogError("[EnemySpawner] prefab 참조가 비어 있습니다.", this);
            if (spawnGroup == null) Debug.LogError("[EnemySpawner] spawnGroup 참조가 비어 있습니다.", this);
        }

        private IDayNight dayNight;
        private bool nightExtrasSpawned;

        private void OnEnable()
        {
            GameServices.Ready += HandleReady;
            if (GameServices.IsReady) HandleReady();
        }

        private void OnDisable()
        {
            GameServices.Ready -= HandleReady;
            if (dayNight != null) dayNight.NightChanged -= HandleNightChanged;
            dayNight = null;
        }

        private void HandleReady()
        {
            if (dayNight != null) return;
            dayNight = GameServices.DayNight;
            if (dayNight != null) dayNight.NightChanged += HandleNightChanged;
        }

        /// <summary>밤이 되면 '밤 추가 수'만큼만 더 내보낸다(5.6 8단계). 낮에 잡은 적은 리스폰 시간이 지나야 돌아온다.
        /// 낮이 돼도 이미 나온 적은 그대로 두고, 다음 밤에 다시 추가분을 낸다.</summary>
        private void HandleNightChanged(bool isNight)
        {
            if (!isNight)
            {
                nightExtrasSpawned = false;
                return;
            }
            if (depleted || nightExtrasSpawned || spawnGroup == null) return;
            nightExtrasSpawned = true;
            SpawnUpTo(alive.Count + spawnGroup.NightExtraCount);
        }

        /// <summary>맵에 들어올 때 내보낼 수. 밤이면 처음부터 추가분까지 낸다.</summary>
        private int InitialCount()
        {
            if (spawnGroup == null) return 0;
            bool night = dayNight != null && dayNight.IsNight;
            if (night) nightExtrasSpawned = true;
            return spawnGroup.MaxAliveCount + (night ? spawnGroup.NightExtraCount : 0);
        }

        private void OnDestroy()
        {
            // 맵 언로드로 스포너와 적이 함께 사라질 때 남은 구독을 정리한다.
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] == null) continue;
                EnemyHealth instanceHealth = alive[i].GetComponent<EnemyHealth>();
                if (instanceHealth != null) instanceHealth.Died -= HandleCreatureDied;
            }
            alive.Clear();
        }

        private void Start()
        {
            // Restore()가 Awake 직후(씬 로드 시) 이미 depleted를 맞춰 놓은 상태다 — 비어 있지 않을 때만 쓴다.
            if (!depleted) SpawnUpTo(InitialCount());
        }

        private void SpawnUpTo(int targetCount)
        {
            if (prefab == null || spawnGroup == null) return;
            float radius = spawnGroup.SpawnRadiusTiles;
            while (alive.Count < targetCount)
            {
                Vector2 offset = radius > 0f ? Random.insideUnitCircle * radius : Vector2.zero;
                CreatureBrain instance = Instantiate(prefab, (Vector2)transform.position + offset, Quaternion.identity);
                alive.Add(instance);

                EnemyHealth instanceHealth = instance.GetComponent<EnemyHealth>();
                if (instanceHealth != null) instanceHealth.Died += HandleCreatureDied;
            }
        }

        private void HandleCreatureDied(EnemyHealth deadHealth)
        {
            deadHealth.Died -= HandleCreatureDied;
            CreatureBrain instance = deadHealth.GetComponent<CreatureBrain>();
            alive.Remove(instance);
            Destroy(deadHealth.gameObject);
            if (alive.Count > 0) return;

            depleted = true;
            long nowMinute = GameServices.WorldClock != null ? GameServices.WorldClock.TotalMinutes : 0;
            int respawnDays = prefab != null && prefab.Definition != null ? prefab.Definition.RespawnDays : 0;
            long respawnMinutes = DifficultyRules.RespawnMinutes(respawnDays, GameServices.Difficulty);
            respawnAtMinute = respawnMinutes > 0 ? nowMinute + respawnMinutes : 0;
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(depleted ? 1 : 0, respawnAtMinute, null);

        public void Restore(ObjectStateRecord record, long nowMinute)
        {
            if (record.State == 0)
            {
                depleted = false;
                respawnAtMinute = 0;
                return;
            }

            // Harvestable.Restore와 같은 이유로, 저장된 리스폰 시각을 그대로 쓴다(다시 계산해 두 배로
            // 늘리지 않는다).
            bool respawnReady = prefab != null && prefab.Definition != null && prefab.Definition.RespawnDays > 0
                                 && nowMinute >= record.UntilMinute;
            depleted = !respawnReady;
            respawnAtMinute = respawnReady ? 0 : record.UntilMinute;
        }
    }
}
