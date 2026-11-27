using Game.Core;
using UnityEngine;

namespace Game.Lore
{
    /// <summary>
    /// 쪽지 하나 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위, ARCHITECTURE.md 4.9). 제목·본문은 기획 내용이라
    /// 에셋은 비공개 Data/Lore/에만 둔다 — 공개 코드·씬에 문구를 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Lore/Lore Note Definition")]
    public sealed class LoreNoteDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string title = "";
        [SerializeField] [TextArea(4, 20)] private string body = "";

        public string Id => id;
        public string Title => title;
        public string Body => body;
    }
}
