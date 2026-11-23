using System;
using System.Collections.Generic;
using Game.Building;
using Game.Core;
using Game.Maps;
using Game.Player;
using Game.World;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 플레이어의 SurvivalStats를 소유하고 WorldClock(허기·갈증·체온·HP)과 실시간(스태미나)으로 갱신한다
    /// (ARCHITECTURE.md 4.3, docs/design/GAME_DESIGN.md 5.2). HandModeChanged로 짐승 손 허기 배율을,
    /// GameServices.Ready 이후 WorldClock·MapLoader로 밤·실내 여부에 따른 체온 모디파이어를 등록한다.
    /// </summary>
    public sealed class PlayerSurvival : MonoBehaviour
    {
        private const string HandModeSource = "hand_mode";
        private const string EnvironmentSource = "environment";

        [SerializeField] private SurvivalStatDatabase database;
        [SerializeField] private SurvivalSettings settings;
        [SerializeField] private PlayerHands hands;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerStatusEffects statusEffects;

        private IWorldClock worldClock;
        private MapLoader mapLoader;
        private SurvivalStats stats;
        private bool diedSignaled;

        // 침대처럼 "HP 자연 회복만 더 빠르게" 하는 출처를 위한 자리. 허기·갈증처럼 Tick 기반 비율이
        // 아니라 ApplyHealthRule의 회복 분기에서만 곱해지므로 SurvivalStats.SetModifier와는 따로 둔다.
        private readonly Dictionary<string, float> hpRegenMultipliers = new Dictionary<string, float>();

        public SurvivalStats Stats => stats ??= new SurvivalStats(database);

        /// <summary>HP가 바닥을 찍은 순간 한 번 발행한다. PlayerDeath가 구독해 부활을 처리한다.</summary>
        public event Action Died;

        private void Awake()
        {
            if (database == null) Debug.LogError("[PlayerSurvival] database 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[PlayerSurvival] settings 참조가 비어 있습니다.", this);
            if (hands == null) Debug.LogError("[PlayerSurvival] hands 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[PlayerSurvival] inputReader 참조가 비어 있습니다.", this);
            if (statusEffects == null) Debug.LogError("[PlayerSurvival] statusEffects 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (hands != null) hands.HandModeChanged += HandleHandModeChanged;
            GameServices.Ready += HandleServicesReady;
            if (GameServices.IsReady) HandleServicesReady();
        }

        private void OnDisable()
        {
            if (hands != null) hands.HandModeChanged -= HandleHandModeChanged;
            GameServices.Ready -= HandleServicesReady;
            if (worldClock != null)
            {
                worldClock.MinutePassed -= HandleMinutePassed;
                worldClock.Advanced -= HandleAdvanced;
                worldClock.HourPassed -= HandleHourPassed;
            }
            if (mapLoader != null) mapLoader.MapChanged -= HandleMapChanged;
        }

        private void Update()
        {
            if (settings == null) return;
            SurvivalStat stamina = Stats.Get(StatIds.Stamina);
            if (stamina == null) return;

            bool wantsRun = inputReader != null && inputReader.RunHeld;
            // 가만히 서서 달리기 키만 누르고 있으면 소모하지 않는다 — 실제로 움직이고 있을 때만 깎는다.
            bool isMoving = inputReader != null && inputReader.MoveInput.sqrMagnitude > 0.0001f;
            bool beast = hands != null && hands.Mode == HandMode.Beast;
            float before = stamina.Value;
            if (wantsRun && isMoving && !stamina.IsAtMin)
            {
                float drain = beast ? settings.StaminaDrainPerSecondBeastRun : settings.StaminaDrainPerSecondRun;
                stamina.Add(-drain * Time.deltaTime);
            }
            else
            {
                bool sick = statusEffects != null && statusEffects.Effects.Has(StatusEffectIds.Sickness);
                float regen = settings.StaminaRegenPerSecond * (sick ? settings.SicknessStaminaRegenMultiplier : 1f);
                stamina.Add(regen * Time.deltaTime);
            }
            // 이미 0이나 최대치에 머물러 있으면(값이 그대로면) HUD를 매 프레임 다시 그리게 하지 않는다.
            if (stamina.Value != before) Stats.NotifyChanged();
        }

        public bool HasStamina => Stats.Get(StatIds.Stamina)?.IsAtMin != true;

        /// <summary>은신처에서 부활할 때 HP를 settings.ReviveHpFraction만큼 채운다.</summary>
        public void Revive()
        {
            SurvivalStat hp = Stats.Get(StatIds.Hp);
            if (hp == null) return;
            hp.SetValue(hp.Definition.Max * (settings != null ? settings.ReviveHpFraction : 0.5f));
            diedSignaled = false;
            Stats.NotifyChanged();
        }

        public void RestoreStats(IEnumerable<(string id, float value)> values) =>
            Stats.RestoreValues(values);

        public IEnumerable<(string id, float value)> EnumerateStats()
        {
            foreach (SurvivalStat stat in Stats.All)
                yield return (stat.Definition.Id, stat.Value);
        }

        /// <summary>침대에서 자는 동안처럼, HP 자연 회복 속도에만 곱해지는 배율을 등록·해제한다.</summary>
        public void SetHpRegenMultiplier(string sourceKey, float multiplier) => hpRegenMultipliers[sourceKey] = multiplier;

        public void ClearHpRegenMultiplier(string sourceKey) => hpRegenMultipliers.Remove(sourceKey);

        private float HpRegenMultiplier
        {
            get
            {
                float result = 1f;
                foreach (float value in hpRegenMultipliers.Values) result *= value;
                return result;
            }
        }

        private void HandleHandModeChanged(HandMode mode)
        {
            if (settings == null) return;
            if (mode == HandMode.Beast)
                Stats.SetModifier(HandModeSource, StatIds.Hunger, StatModifier.Multiply(settings.BeastHandHungerMultiplier));
            else
                Stats.ClearModifier(HandModeSource, StatIds.Hunger);
        }

        private void HandleServicesReady()
        {
            worldClock = GameServices.WorldClock;
            mapLoader = GameServices.MapLoader;
            if (worldClock != null)
            {
                worldClock.MinutePassed += HandleMinutePassed;
                worldClock.Advanced += HandleAdvanced;
                worldClock.HourPassed += HandleHourPassed;
            }
            if (mapLoader != null) mapLoader.MapChanged += HandleMapChanged;
            RefreshTemperatureModifier();
        }

        private void HandleMinutePassed()
        {
            TickGameTime(1f / 60f);
            RefreshTemperatureModifier();
        }

        /// <summary>잠자기·먼 지역 이동처럼 한 번에 몇 시간씩 건너뛸 때, 그 시간을 통째로 한 번에
        /// 계산하지 않고 1시간 단위로 잘라서 돈다 — 그래야 허기·갈증이 중간에 0을 지나는 시점부터
        /// 정확히 "굶는 상태"로 HP가 깎인다(통째로 계산하면 마지막 상태만 보고 전체 시간에 그 규칙을
        /// 적용해 버린다).</summary>
        private void HandleAdvanced(long minutes)
        {
            long wholeHours = minutes / 60;
            long remainderMinutes = minutes % 60;
            for (long i = 0; i < wholeHours; i++) TickGameTime(1f);
            if (remainderMinutes > 0) TickGameTime(remainderMinutes / 60f);
            RefreshTemperatureModifier();
        }

        private void HandleHourPassed() => RefreshTemperatureModifier();

        private void HandleMapChanged(MapDefinition _) => RefreshTemperatureModifier();

        private void TickGameTime(float gameHours)
        {
            Stats.Tick(gameHours);
            ApplyHealthRule(gameHours);
        }

        private void ApplyHealthRule(float gameHours)
        {
            if (settings == null) return;
            SurvivalStat hp = Stats.Get(StatIds.Hp);
            SurvivalStat hunger = Stats.Get(StatIds.Hunger);
            SurvivalStat thirst = Stats.Get(StatIds.Thirst);
            SurvivalStat temperature = Stats.Get(StatIds.Temperature);
            if (hp == null) return;

            bool starving = (hunger != null && hunger.IsAtMin) || (thirst != null && thirst.IsAtMin);
            bool freezing = temperature != null && temperature.IsAtMin;
            bool hasBleeding = statusEffects != null && statusEffects.Effects.Has(StatusEffectIds.Bleeding);
            bool hasSickness = statusEffects != null && statusEffects.Effects.Has(StatusEffectIds.Sickness);

            float delta = 0f;
            if (starving) delta += settings.HpDrainPerHourWhenStarving;
            if (freezing) delta += settings.HpDrainPerHourWhenCold;
            if (!starving && !freezing && !hasBleeding && !hasSickness
                && (hunger == null || hunger.Value >= settings.HpRegenMinHunger)
                && (thirst == null || thirst.Value >= settings.HpRegenMinThirst))
                delta += settings.HpRegenPerHourWhenHealthy * HpRegenMultiplier;

            if (delta != 0f)
            {
                hp.Add(delta * gameHours);
                Stats.NotifyChanged();
            }

            CheckDeath();
        }

        /// <summary>HP가 0인지 지금 바로 확인해서 "막 0이 된 전이"에서만 한 번 Died를 쏜다. 시계 틱
        /// (ApplyHealthRule)뿐 아니라 FallDamage처럼 HP를 바로 깎는 곳도 그 직후 이걸 불러야 한다 —
        /// 다음 분이 지날 때까지 사망 판정이 늦어지면 안 된다.</summary>
        public void CheckDeath()
        {
            SurvivalStat hp = Stats.Get(StatIds.Hp);
            if (hp == null) return;

            // hp.IsAtMin은 레벨 조건이라 0에 머무는 동안 매번 참이 된다. "막 0이 된 전이"에서만 한 번 발행한다.
            if (hp.IsAtMin)
            {
                if (!diedSignaled)
                {
                    diedSignaled = true;
                    Died?.Invoke();
                }
            }
            else
            {
                diedSignaled = false;
            }
        }

        private void RefreshTemperatureModifier()
        {
            if (settings == null || worldClock == null) return;
            bool safe = mapLoader?.CurrentMap != null &&
                        (mapLoader.CurrentMap.IsIndoor || mapLoader.CurrentMap.Kind == MapKind.Hub);
            int hour = (int)(worldClock.TotalMinutes / 60 % 24);
            float heatBonus = GameServices.HeatZoneRegistry != null
                ? GameServices.HeatZoneRegistry.GetExtraRecoveryAt(transform.position)
                : 0f;

            // 실외 밤이 아니면(실내·은신처, 실외 낮) 서서히 회복한다 — 최대치에서 멈추는 건
            // SurvivalStat.Add의 clamp가 처리한다. 켜진 화로 근처면 heatBonus만큼 더 빠르다.
            Stats.SetModifier(EnvironmentSource, StatIds.Temperature, settings.ComputeTemperatureModifier(safe, hour, heatBonus));
        }
    }
}
