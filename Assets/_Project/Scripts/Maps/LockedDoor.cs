using Game.Combat;
using Game.Core;
using Game.Inventory;
using Game.Player;
using UnityEngine;

namespace Game.Maps
{
    /// <summary>
    /// 맵에 손으로 놓은 잠긴 문 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위, ARCHITECTURE.md 3.4).
    /// - 열쇠(문마다 짝): 사람 손으로 조용히 연다. 쓴 열쇠는 가방에서 사라진다.
    /// - 쇠지렛대: 열쇠 없이 연다. 눌러 담는 시간이 걸리고 작은 소음이 나며 내구도가 준다.
    /// - 무기로 치기(IDamageable): 흔들리기만 하고 부서지지 않는다. 대신 큰 소음을 낸다.
    /// 연 문은 영구히 열려 있다(4.2) — 다시 닫는 기능은 없다. State: 0=잠김, 1=열쇠로 엶, 2=쇠지렛대로 엶.
    /// 막는 콜라이더는 Ground 레이어(손으로 놓은 지형, RENDERING_RULES.md 3장), 상호작용·타격 판정은
    /// Interactable 레이어 트리거가 맡는다.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class LockedDoor : MonoBehaviour, IInteractable, IPersistentObject, IDamageable
    {
        private const int StateLocked = 0;
        private const int StateOpenedByKey = 1;
        private const int StateOpenedByCrowbar = 2;
        // 흔들림 연출의 진동 속도(기술 값, 표시용).
        private const float ShakeFrequency = 60f;

        [SerializeField] private PersistentId persistentId;
        [SerializeField] private ExplorationSettings settings;
        [Tooltip("이 문에 맞는 열쇠 아이템 id. 문마다 다른 열쇠를 둔다.")]
        [SerializeField] private string keyItemId = "";
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private Collider2D interactTrigger;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private string lockedPrompt = "잠겨 있다";
        [SerializeField] private string keyPrompt = "열쇠로 열기";
        [SerializeField] private string crowbarPrompt = "쇠지렛대로 열기";

        private int state;
        private float shakeRemaining;
        private Vector3 visualRestPosition;
        private PlayerHands playerHands;
        private PlayerHeldItem playerHeldItem;
        private PlayerInventory playerInventory;
        private bool playerLookupFailedLogged;

        public string Id => persistentId != null ? persistentId.Id : "";
        public bool IsOpen => state != StateLocked;
        public HandRequirement RequiredHand => HandRequirement.Any;

        public string InteractPrompt => CurrentMethod() switch
        {
            UnlockMethod.Key => keyPrompt,
            UnlockMethod.Crowbar => crowbarPrompt,
            _ => lockedPrompt
        };

        public float HoldSecondsRequired =>
            CurrentMethod() == UnlockMethod.Crowbar && settings != null ? settings.CrowbarHoldSeconds : 0f;

        private void Awake()
        {
            if (persistentId == null) Debug.LogError("[LockedDoor] persistentId 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[LockedDoor] settings 참조가 비어 있습니다.", this);
            if (blockingCollider == null) Debug.LogError("[LockedDoor] blockingCollider 참조가 비어 있습니다.", this);
            if (interactTrigger == null) Debug.LogError("[LockedDoor] interactTrigger 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[LockedDoor] visual 참조가 비어 있습니다.", this);
            if (string.IsNullOrEmpty(keyItemId)) Debug.LogError("[LockedDoor] keyItemId가 비어 있습니다.", this);
            if (visual != null) visualRestPosition = visual.transform.localPosition;
            RefreshVisual();
        }

        private void Update()
        {
            if (shakeRemaining <= 0f || visual == null) return;
            shakeRemaining -= Time.deltaTime;
            float amplitude = settings != null ? settings.DoorShakeAmplitudeTiles : 0f;
            float offset = shakeRemaining > 0f ? Mathf.Sin(shakeRemaining * ShakeFrequency) * amplitude : 0f;
            visual.transform.localPosition = visualRestPosition + new Vector3(offset, 0f, 0f);
        }

        public void Interact(GameObject interactor)
        {
            switch (CurrentMethod())
            {
                case UnlockMethod.Key:
                    playerInventory.Remove(keyItemId, 1);
                    SetState(StateOpenedByKey);
                    break;
                case UnlockMethod.Crowbar:
                    if (playerHeldItem.Inventory != null)
                        playerHeldItem.Inventory.Model.DamageTool(playerHeldItem.SelectedSlotIndex,
                            settings != null ? settings.CrowbarDurabilityCost : 0);
                    EmitNoise(settings != null ? settings.CrowbarNoiseRadiusTiles : 0f, NoiseKind.Crowbar);
                    SetState(StateOpenedByCrowbar);
                    break;
            }
        }

        /// <summary>무기·발톱으로 치면 흔들리기만 하고 큰 소음이 난다(부서지지 않는다).</summary>
        public void TakeDamage(DamageInfo info)
        {
            if (IsOpen || settings == null) return;
            shakeRemaining = settings.DoorShakeSeconds;
            EmitNoise(settings.DoorBashNoiseRadiusTiles, NoiseKind.DoorBash);
        }

        public ObjectStateRecord Capture() => new ObjectStateRecord(state, 0, null);

        public void Restore(ObjectStateRecord record, long nowMinute) =>
            SetState(record.State >= StateLocked && record.State <= StateOpenedByCrowbar ? record.State : StateLocked);

        private UnlockMethod CurrentMethod()
        {
            if (IsOpen || !ResolvePlayer()) return UnlockMethod.None;
            bool hasKey = !string.IsNullOrEmpty(keyItemId) && playerInventory.CountOf(keyItemId) > 0;
            bool human = playerHands.Mode == HandMode.Human && !playerHands.IsTransitioning;
            int durability = playerHeldItem.SelectedSlot?.Durability ?? -1;
            return LockedDoorRules.Resolve(human, hasKey, playerHeldItem.SelectedDefinition, durability);
        }

        /// <summary>플레이어 참조는 상주 씬(Boot)에 있어 Awake에서 못 받는다 — 처음 필요할 때 한 번만 찾는다
        /// (GetComponent는 이때 한 번뿐, hot path 아님).</summary>
        private bool ResolvePlayer()
        {
            if (playerInventory != null) return playerHands != null && playerHeldItem != null;
            playerInventory = GameServices.PlayerInventory;
            if (playerInventory == null) return false;
            playerHands = playerInventory.GetComponentInParent<PlayerHands>();
            playerHeldItem = playerInventory.GetComponentInParent<PlayerHeldItem>();
            bool ok = playerHands != null && playerHeldItem != null;
            if (!ok && !playerLookupFailedLogged)
            {
                playerLookupFailedLogged = true;
                Debug.LogError("[LockedDoor] 플레이어에서 PlayerHands/PlayerHeldItem을 찾지 못해 문이 반응하지 않습니다.", this);
            }
            return ok;
        }

        private void EmitNoise(float radius, NoiseKind kind)
        {
            if (radius > 0f) GameServices.NoiseSystem?.Emit(transform.position, radius, kind);
        }

        private void SetState(int value)
        {
            state = value;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool open = IsOpen;
            if (blockingCollider != null) blockingCollider.enabled = !open;
            if (interactTrigger != null) interactTrigger.enabled = !open;
            if (visual != null) visual.sprite = open ? openSprite : lockedSprite;
        }
    }
}
