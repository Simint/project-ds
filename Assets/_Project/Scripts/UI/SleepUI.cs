using System;
using Game.Building;
using Game.Core;
using Game.Player;
using Game.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 잘 시간 고르기 창 (docs/design/GAME_DESIGN.md 8, MVP 5단계). 자는 동안 허기·갈증은 덜 줄고 HP는
    /// 더 빨리 회복하는 모디파이어를 걸고 WorldClock.Advance로 시간을 건너뛴다. 허기·갈증이 이미
    /// 0이거나, 출혈 중이거나, 골절이 치료되지 않았으면 못 잔다(SleepRules). 자는 중간에 허기·갈증이
    /// 0이 될 것 같으면 그 직전까지만 재운다(깬 것으로 취급).
    /// </summary>
    public sealed class SleepUI : MonoBehaviour
    {
        private const string SleepSource = "sleep";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button shortSleepButton;
        [SerializeField] private Button mediumSleepButton;
        [SerializeField] private Button longSleepButton;
        [SerializeField] private Text warningText;
        [SerializeField] private SleepSettings settings;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private string bleedingWarning = "피가 멎지 않아 잘 수 없습니다 (붕대로 치료하세요)";
        [SerializeField] private string fractureWarning = "뼈가 부러져 잘 수 없습니다 (부목 등으로 치료하세요)";

        private PlayerSurvival survival;
        private PlayerStatusEffects statusEffects;
        private string starvingWarning = "";
        private int openedFrame = -1;

        private void Awake()
        {
            if (panelRoot == null) Debug.LogError("[SleepUI] panelRoot 참조가 비어 있습니다.", this);
            if (shortSleepButton == null) Debug.LogError("[SleepUI] shortSleepButton 참조가 비어 있습니다.", this);
            if (mediumSleepButton == null) Debug.LogError("[SleepUI] mediumSleepButton 참조가 비어 있습니다.", this);
            if (longSleepButton == null) Debug.LogError("[SleepUI] longSleepButton 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[SleepUI] settings 참조가 비어 있습니다.", this);
            if (inputReader == null) Debug.LogError("[SleepUI] inputReader 참조가 비어 있습니다.", this);

            // 허기·갈증 안내는 씬에 적어 둔 문구를 그대로 쓴다. 출혈·골절일 때만 문구를 바꿔 끼운다.
            if (warningText != null) starvingWarning = warningText.text;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.UICancelPressed += HandleCancelPressed;
            shortSleepButton?.onClick.AddListener(HandleShortSleepClicked);
            mediumSleepButton?.onClick.AddListener(HandleMediumSleepClicked);
            longSleepButton?.onClick.AddListener(HandleLongSleepClicked);
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.UICancelPressed -= HandleCancelPressed;
                inputReader.SetLocked(this, false); // 열려 있는 채로 비활성화돼도 잠금이 영원히 남지 않게 한다
            }
            shortSleepButton?.onClick.RemoveListener(HandleShortSleepClicked);
            mediumSleepButton?.onClick.RemoveListener(HandleMediumSleepClicked);
            longSleepButton?.onClick.RemoveListener(HandleLongSleepClicked);
        }

        public void Show(PlayerSurvival targetSurvival, PlayerStatusEffects targetStatusEffects)
        {
            if (panelRoot == null) return;
            survival = targetSurvival;
            statusEffects = targetStatusEffects;

            SleepBlockReason reason = EvaluateBlockReason();
            bool canSleep = reason == SleepBlockReason.None;
            if (shortSleepButton != null) shortSleepButton.interactable = canSleep;
            if (mediumSleepButton != null) mediumSleepButton.interactable = canSleep;
            if (longSleepButton != null) longSleepButton.interactable = canSleep;
            if (warningText != null)
            {
                warningText.gameObject.SetActive(!canSleep);
                if (!canSleep) warningText.text = WarningFor(reason);
            }

            panelRoot.SetActive(true);
            openedFrame = Time.frameCount;
            inputReader?.SetLocked(this, true);
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            inputReader?.SetLocked(this, false);
            survival = null;
            statusEffects = null;
        }

        private void HandleCancelPressed()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            // Interact(패드 East)와 같은 물리 버튼이라, 창을 연 바로 그 입력에 바로 닫히는 걸 막는다
            // (TravelMenuUI와 같은 이유).
            if (Time.frameCount == openedFrame) return;
            Hide();
        }

        private bool CanSleepAtAll() => survival != null && EvaluateBlockReason() == SleepBlockReason.None;

        private SleepBlockReason EvaluateBlockReason()
        {
            if (survival == null) return SleepBlockReason.Starving;
            SurvivalStat hunger = survival.Stats.Get(StatIds.Hunger);
            SurvivalStat thirst = survival.Stats.Get(StatIds.Thirst);
            bool starving = (hunger != null && hunger.IsAtMin) || (thirst != null && thirst.IsAtMin);
            bool bleeding = statusEffects != null && statusEffects.Effects.Has(StatusEffectIds.Bleeding);
            bool fracture = statusEffects != null && statusEffects.Effects.Has(StatusEffectIds.Fracture);
            return SleepRules.Evaluate(starving, bleeding, fracture);
        }

        private string WarningFor(SleepBlockReason reason) => reason switch
        {
            SleepBlockReason.Bleeding => bleedingWarning,
            SleepBlockReason.Fracture => fractureWarning,
            _ => starvingWarning
        };

        private void HandleShortSleepClicked() => Sleep(settings.ShortSleepHours);
        private void HandleMediumSleepClicked() => Sleep(settings.MediumSleepHours);
        private void HandleLongSleepClicked() => Sleep(settings.LongSleepHours);

        private void Sleep(int requestedHours)
        {
            if (survival == null || !CanSleepAtAll()) { Hide(); return; }

            long actualMinutes = ClampToAvoidStarvation(requestedHours * 60L);
            if (actualMinutes <= 0) { Hide(); return; }

            survival.Stats.SetModifier(SleepSource, StatIds.Hunger, StatModifier.Multiply(settings.HungerThirstDrainMultiplier));
            survival.Stats.SetModifier(SleepSource, StatIds.Thirst, StatModifier.Multiply(settings.HungerThirstDrainMultiplier));
            survival.SetHpRegenMultiplier(SleepSource, settings.HpRegenMultiplier);

            GameServices.WorldClock?.Advance(actualMinutes);

            survival.Stats.ClearModifier(SleepSource, StatIds.Hunger);
            survival.Stats.ClearModifier(SleepSource, StatIds.Thirst);
            survival.ClearHpRegenMultiplier(SleepSource);

            GameServices.MapLoader?.SaveNow();
            Hide();
        }

        /// <summary>요청한 시간 동안 자면 허기·갈증이 0 밑으로 떨어질 것 같으면, 0이 되는 시점(1분
        /// 여유를 둔 그 직전)까지만 재운다(=중간에 깬 것으로 취급). 시간 단위가 아니라 분 단위로 계산해서,
        /// "정확히 몇 시간" 단위로만 끊어 재우느라 실제로 잘 수 있는 시간보다 덜 자는 경우를 줄인다.</summary>
        private long ClampToAvoidStarvation(long requestedMinutes)
        {
            survival.Stats.SetModifier(SleepSource, StatIds.Hunger, StatModifier.Multiply(settings.HungerThirstDrainMultiplier));
            survival.Stats.SetModifier(SleepSource, StatIds.Thirst, StatModifier.Multiply(settings.HungerThirstDrainMultiplier));

            long maxMinutes = Math.Min(requestedMinutes, MinutesUntilZero(StatIds.Hunger));
            maxMinutes = Math.Min(maxMinutes, MinutesUntilZero(StatIds.Thirst));

            survival.Stats.ClearModifier(SleepSource, StatIds.Hunger);
            survival.Stats.ClearModifier(SleepSource, StatIds.Thirst);
            return Math.Max(0, maxMinutes);
        }

        /// <summary>그 스탯이 0에 닿기까지 남은 분. 1분을 미리 빼서, 오차로 정확히 0에 닿는 그 분에
        /// 걸치는 일이 없게 한다.</summary>
        private long MinutesUntilZero(string statId)
        {
            SurvivalStat stat = survival.Stats.Get(statId);
            if (stat == null) return long.MaxValue;
            float ratePerHour = survival.Stats.EffectiveRatePerHour(statId);
            if (ratePerHour >= 0f) return long.MaxValue;
            long minutes = (long)(stat.Value / -ratePerHour * 60f) - 1;
            return Math.Max(0, minutes);
        }
    }
}
