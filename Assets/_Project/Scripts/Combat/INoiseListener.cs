using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 소음을 듣는 쪽 (적 AI, ARCHITECTURE.md 4.7). NoiseSystem은 위치 목록을 들고 있지 않는다(2장
    /// 원칙 9 "등록부") — 자기 위치와 반경을 비교하는 건 구현하는 쪽이 한다.
    /// </summary>
    public interface INoiseListener
    {
        void OnNoiseHeard(Vector2 position, float radius, NoiseKind kind);
    }
}
