using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 문 (ARCHITECTURE.md 4.5, add-structure 스킬 0절 Door 규칙). 열림 여부는 StructureInstance.State에
    /// 저장한다(0=닫힘, 1=열림) — 다른 구조물처럼 셀 좌표·정의 id와 함께 저장·복원된다. 닫히면 Structure
    /// 레이어 콜라이더로 막고, 열리면 콜라이더를 꺼서 지나갈 수 있게 한다. State는 BuildGrid가 Instantiate
    /// 직후(Awake/OnEnable 이후, Start 이전) 복원값으로 덮어쓰므로, 여기서는 Start에서 한 번 반영한다
    /// (ARCHITECTURE.md "다른 서비스 접근은 Start 이후").
    /// </summary>
    [RequireComponent(typeof(StructureInstance))]
    public sealed class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private string openPrompt = "열기";
        [SerializeField] private string closePrompt = "닫기";

        private StructureInstance structureInstance;

        public string InteractPrompt => IsOpen ? closePrompt : openPrompt;
        public HandRequirement RequiredHand => HandRequirement.Any;
        public float HoldSecondsRequired => 0f;

        public bool IsOpen => structureInstance != null && structureInstance.State == 1;

        private void Awake()
        {
            structureInstance = GetComponent<StructureInstance>();
            if (doorCollider == null) Debug.LogError("[Door] doorCollider 참조가 비어 있습니다.", this);
            if (visual == null) Debug.LogError("[Door] visual 참조가 비어 있습니다.", this);
            if (closedSprite == null) Debug.LogError("[Door] closedSprite 참조가 비어 있습니다.", this);
            if (openSprite == null) Debug.LogError("[Door] openSprite 참조가 비어 있습니다.", this);
        }

        private void Start() => RefreshVisual();

        public void Interact(GameObject interactor)
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Open()
        {
            structureInstance.State = 1;
            RefreshVisual();
        }

        private void Close()
        {
            if (!CanClose()) return;
            structureInstance.State = 0;
            RefreshVisual();
        }

        /// <summary>플레이어가 문 칸 안에 서 있으면 닫을 수 없다(끼임 방지). doorCollider.bounds는 쓰지
        /// 않는다 — 문이 열려 콜라이더가 꺼진 상태(끼임 방지가 실제로 필요한 그 순간)에는 Collider2D가
        /// 크기 0인 bounds를 돌려주기 때문이다(비활성 콜라이더는 물리 세계에서 빠진다). 대신 Footprint로
        /// 셀 영역을 계산한다 — doorCollider의 size·offset은 이 Footprint와 같아야 한다(add-structure
        /// 스킬 0절 Door 규칙).</summary>
        public bool CanClose()
        {
            Vector2Int footprint = structureInstance.Definition != null ? structureInstance.Definition.Footprint : Vector2Int.one;
            Vector2 size = new Vector2(footprint.x, footprint.y);
            Vector2 center = (Vector2)transform.position + size * 0.5f;
            return !Physics2D.OverlapBox(center, size * 0.95f, 0f, GameLayers.PlayerMask);
        }

        /// <summary>콜라이더·스프라이트를 현재 State에 맞춘다. 복원 직후(Start)와 토글 직후에 부른다 —
        /// BuildGrid가 StructureInstance.State를 Instantiate 이후(Awake/OnEnable 다음, Start 전)에
        /// 되돌리므로 Start에서 반영하면 항상 복원된 값을 본다. 테스트에서 복원 상황을 흉내 낼 때도
        /// 직접 부를 수 있도록 public이다.</summary>
        public void RefreshVisual()
        {
            bool open = IsOpen;
            if (doorCollider != null) doorCollider.enabled = !open;
            if (visual != null) visual.sprite = open ? openSprite : closedSprite;
        }
    }
}
