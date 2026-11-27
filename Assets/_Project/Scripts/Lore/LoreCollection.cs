using System;
using System.Collections.Generic;

namespace Game.Lore
{
    /// <summary>
    /// 주운 쪽지 기록 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위, ARCHITECTURE.md 4.9). 가방과 따로라 칸·무게를
    /// 차지하지 않는다. 주운 순서대로 id만 들고, 저장 대상이다. 순수 C# 클래스라 EditMode로 테스트한다.
    /// </summary>
    public sealed class LoreCollection
    {
        private readonly List<string> ids = new List<string>();

        public IReadOnlyList<string> Ids => ids;

        public event Action Changed;

        public bool Contains(string id) => !string.IsNullOrEmpty(id) && ids.Contains(id);

        /// <summary>처음 주운 쪽지면 true. 같은 쪽지를 두 번 넣지 않는다.</summary>
        public bool Add(string id)
        {
            if (string.IsNullOrEmpty(id) || ids.Contains(id)) return false;
            ids.Add(id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>불러오기. 빈 값과 중복은 건너뛴다.</summary>
        public void SetFromSave(IEnumerable<string> saved)
        {
            ids.Clear();
            if (saved != null)
            {
                foreach (string id in saved)
                    if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) ids.Add(id);
            }
            Changed?.Invoke();
        }
    }
}
