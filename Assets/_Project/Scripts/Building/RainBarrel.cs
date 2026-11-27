using Game.Core;
using Game.Inventory;
using Game.Items;
using Game.Player;
using Game.World;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 빗물 통 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위, add-structure 스킬 Prop). 비(폭우 포함)가 오면 더러운 물이 차오른다.
    /// 빈 물병을 들고 쓰면 더러운 물병 하나를 채운다 — 마시려면 화로에서 끓인다(Stove).
    /// 은신처에 없는 동안이나 잠으로 건너뛴 시간에 내린 비도 받는다: WeatherSystem.RainMinutesTotal(실제로 비가 온 분의
    /// 누적)에서 마지막으로 본 값과의 차이만큼 채운다. 저장은 다른 구조물처럼 StructureInstance에 한다 —
    /// State = 차 있는 물병 수, UntilMinute = 마지막으로 본 누적 비 시간 + 1(0이면 막 지은 통), Extra = 1병이 되기 전 남은 양.
    /// </summary>
    public sealed class RainBarrel : MonoBehaviour, IInteractable
    {
        [SerializeField] private RainBarrelSettings settings;
        [SerializeField] private string promptFormat = "빗물 통 ({0}/{1}) — 빈 물병으로 받기";

        private StructureInstance structureInstance;
        private IWorldClock clock;
        private float partialBottles;
        private bool restored;

        public string InteractPrompt => string.Format(promptFormat, StoredBottles, settings != null ? settings.CapacityBottles : 0);
        public HandRequirement RequiredHand => HandRequirement.Human;
        public float HoldSecondsRequired => 0f;
        public int StoredBottles => structureInstance != null ? structureInstance.State : 0;

        private void Awake()
        {
            structureInstance = GetComponent<StructureInstance>();
            if (structureInstance == null) Debug.LogError("[RainBarrel] StructureInstance가 없습니다.", this);
            if (settings == null) Debug.LogError("[RainBarrel] settings 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            GameServices.Ready += HandleReady;
            if (GameServices.IsReady) HandleReady();
        }

        private void OnDisable()
        {
            GameServices.Ready -= HandleReady;
            if (clock == null) return;
            clock.MinutePassed -= Refresh;
            clock.Advanced -= HandleAdvanced;
            clock = null;
        }

        private void HandleReady()
        {
            if (clock != null) return;
            clock = GameServices.WorldClock;
            if (clock == null) return;
            clock.MinutePassed += Refresh;
            clock.Advanced += HandleAdvanced;
        }

        /// <summary>BuildGrid가 Instantiate 뒤(Start 전)에 State·UntilMinute·Extra를 되돌리므로, 첫 계산은 Start에서 한다.</summary>
        private void Start() => Refresh();

        private void HandleAdvanced(long minutes) => Refresh();

        private void Refresh()
        {
            if (structureInstance == null || settings == null || GameServices.Weather == null) return;
            long total = GameServices.Weather.RainMinutesTotal;
            if (!restored)
            {
                restored = true;
                float.TryParse(structureInstance.Extra, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out partialBottles);
                if (structureInstance.UntilMinute <= 0) structureInstance.UntilMinute = total + 1; // 막 지은 통은 이전 비를 받지 않는다.
            }

            long seen = structureInstance.UntilMinute - 1;
            long rainedMinutes = total - seen;
            if (rainedMinutes <= 0) return;
            structureInstance.UntilMinute = total + 1;

            (int stored, float partial) = RainBarrelRules.Fill(StoredBottles, partialBottles, settings.CapacityBottles,
                settings.BottlesPerRainHour, rainedMinutes);
            bool partialChanged = !Mathf.Approximately(partial, partialBottles);
            structureInstance.State = stored;
            partialBottles = partial;
            if (partialChanged) structureInstance.Extra = partial.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Interact(GameObject interactor)
        {
            Refresh();
            if (structureInstance == null || StoredBottles <= 0) return;
            PlayerHeldItem heldItem = interactor != null ? interactor.GetComponentInParent<PlayerHeldItem>() : null;
            if (heldItem == null || heldItem.Inventory == null) return;
            ItemDefinition selected = heldItem.SelectedDefinition;
            if (selected == null || selected.Id != WaterItemIds.EmptyBottle) return;

            int taken = heldItem.Inventory.Model.RemoveFromSlot(heldItem.SelectedSlotIndex, 1);
            if (taken <= 0) return;
            heldItem.Inventory.Collect(WaterItemIds.DirtyWaterBottle, 1);
            structureInstance.State = StoredBottles - 1;
        }
    }
}
