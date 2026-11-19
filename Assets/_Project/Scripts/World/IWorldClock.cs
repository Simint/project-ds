using System;

namespace Game.World
{
    /// <summary>게임 시간의 유일한 출처. 다른 시스템은 이 인터페이스로 받아 테스트에서 가짜 시계로 바꿀 수 있다.</summary>
    public interface IWorldClock
    {
        long TotalMinutes { get; }

        /// <summary>1분마다. 분마다 쏘지 않는 Advance와 달리 평상시 진행에만 쓴다.</summary>
        event Action MinutePassed;
        event Action HourPassed;
        event Action DayChanged;

        /// <summary>맵 이동·잠자기처럼 시간을 건너뛸 때. 분마다 이벤트를 쏘지 않고 한 번에 알린다.</summary>
        event Action<long> Advanced;

        void Advance(long minutes);
    }
}
