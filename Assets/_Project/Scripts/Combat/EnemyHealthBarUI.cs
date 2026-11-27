using UnityEngine;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// 적 머리 위 체력바. 맞았을 때만 잠깐 떴다 사라진다 (docs/design/GAME_DESIGN.md 6.5). 월드 스페이스
    /// 캔버스를 자식으로 둔 간단한 표시용 컴포넌트라 MonoBehaviour로 둔다.
    /// </summary>
    public sealed class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private CombatSettings settings;
        [SerializeField] private GameObject root;
        [SerializeField] private Image fillImage;

        private float visibleRemainingSeconds;

        private void Awake()
        {
            if (enemyHealth == null) Debug.LogError("[EnemyHealthBarUI] enemyHealth 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[EnemyHealthBarUI] settings 참조가 비어 있습니다.", this);
            if (root == null) Debug.LogError("[EnemyHealthBarUI] root 참조가 비어 있습니다.", this);
            if (fillImage == null) Debug.LogError("[EnemyHealthBarUI] fillImage 참조가 비어 있습니다.", this);
            if (root != null) root.SetActive(false);
        }

        private void OnEnable()
        {
            if (enemyHealth != null) enemyHealth.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (enemyHealth != null) enemyHealth.Damaged -= HandleDamaged;
        }

        private void Update()
        {
            if (visibleRemainingSeconds <= 0f) return;
            visibleRemainingSeconds -= Time.deltaTime;
            if (visibleRemainingSeconds <= 0f && root != null) root.SetActive(false);
        }

        private void HandleDamaged(float current, float max)
        {
            if (fillImage != null) fillImage.fillAmount = max > 0f ? current / max : 0f;
            if (root != null) root.SetActive(true);
            visibleRemainingSeconds = settings != null ? settings.EnemyHealthBarVisibleSeconds : 0f;
        }
    }
}
