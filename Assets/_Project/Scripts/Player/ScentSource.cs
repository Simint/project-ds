using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 후각으로 찾을 수 있는 대상 (땅 파기 지점, 떨어진 아이템·상자, 적 — docs/design/GAME_DESIGN.md 3.2).
    /// 켜져 있는 동안만 ScentSystem 등록부에 들어간다(ARCHITECTURE.md 2장 원칙 9) — 다 판 지점이나 털린
    /// 상자는 이 컴포넌트를 꺼서 더 이상 냄새가 나지 않게 한다.
    /// </summary>
    public sealed class ScentSource : MonoBehaviour
    {
        [SerializeField] private ScentKind kind = ScentKind.Item;

        public ScentKind Kind => kind;
        public Vector2 Position => transform.position;

        private void OnEnable() => GameServices.ScentSystem?.Register(this);

        private void OnDisable() => GameServices.ScentSystem?.Unregister(this);
    }
}
