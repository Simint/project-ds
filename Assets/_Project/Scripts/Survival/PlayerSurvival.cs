using System;
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
            bool beast = hands != null && hands.Mode == HandMode.Beast;
            float before = stamina.Value;
            if (wantsRun && !stamina.IsAtMin)
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

        public void RestoreStats(System.Collections.Generic.IEnumerable<(string id, float value)> values) =>
            Stats.RestoreValues(values);

        public System.Collections.Generic.IEnumerable<(string id, float value)> EnumerateStats()
        {
            foreach (SurvivalStat stat in Stats.All)
                yield return (stat.Definition.Id, stat.Value);
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

        private void HandleMinutePassed() => TickGameTime(1f / 60f);

        private void HandleAdvanced(long minutes) => TickGameTime(minutes / 60f);

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
                delta += settings.HpRegenPerHourWhenHealthy;

            if (delta != 0f)
            {
                hp.Add(delta * gameHours);
                Stats.NotifyChanged();
            }

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
            bool night = settings.IsNightHour(hour);

            if (!safe && night)
                Stats.SetModifier(EnvironmentSource, StatIds.Temperature,
                    StatModifier.Additive(settings.NightOutdoorTemperatureDropPerHour));
            else
                Stats.ClearModifier(EnvironmentSource, StatIds.Temperature);
        }
    }
}
