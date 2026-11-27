using System;

namespace Game.World
{
    /// <summary>지금이 밤인지 알려 주는 서비스 (ARCHITECTURE.md 4.1 DayNightCycle). 적·생존 시스템이 구독한다.</summary>
    public interface IDayNight
    {
        bool IsNight { get; }
        /// <summary>낮(1)~밤(0) 밝기 비율.</summary>
        float Daylight { get; }
        /// <summary>밤이 되거나(true) 낮이 될(false) 때.</summary>
        event Action<bool> NightChanged;
    }
}
