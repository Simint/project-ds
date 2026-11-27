using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 후각 탐지 대상 고르기 (docs/design/GAME_DESIGN.md 3.2). 반경 안이고, 벽에 가로막히지 않은 것만 고른다
    /// ("벽 너머의 물건 감지는 넣지 않는다"). 가로막힘 판정은 바깥에서 넘겨 테스트에서 바꿀 수 있게 했다.
    /// 결과는 호출하는 쪽이 준비한 버퍼에 채워 할당을 만들지 않는다.
    /// </summary>
    public static class ScentQuery
    {
        public static int Select(Vector2 origin, float radius, int count, Func<int, Vector2> positionAt,
                                 Func<Vector2, Vector2, bool> isBlocked, int[] results)
        {
            if (radius <= 0f || results == null) return 0;
            float sqrRadius = radius * radius;
            int found = 0;
            for (int i = 0; i < count && found < results.Length; i++)
            {
                Vector2 position = positionAt(i);
                if ((position - origin).sqrMagnitude > sqrRadius) continue;
                if (isBlocked != null && isBlocked(origin, position)) continue;
                results[found++] = i;
            }
            return found;
        }
    }
}
