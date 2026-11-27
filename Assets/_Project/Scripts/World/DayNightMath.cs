using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 낮밤 단계 계산 (docs/design/GAME_DESIGN.md 5.1, 5.6). 해 뜨는 시각 &lt; 해 지는 시각이라고 본다(낮이 자정을 넘지 않음).
    /// 순수 함수라 EditMode 테스트로 검증한다.
    /// </summary>
    public static class DayNightMath
    {
        public static int MinuteOfDay(long totalMinutes)
        {
            long m = totalMinutes % WorldClock.MinutesPerDay;
            return (int)(m < 0 ? m + WorldClock.MinutesPerDay : m);
        }

        public static bool IsNight(int minuteOfDay, int sunrise, int sunset) => minuteOfDay < sunrise || minuteOfDay >= sunset;

        /// <summary>낮이면 1, 밤이면 0. 해 뜰 녘·질 녘에는 transition 분 동안 선형으로 바뀐다(그 시각을 가운데로).</summary>
        public static float Daylight(int minuteOfDay, int sunrise, int sunset, int transition)
        {
            if (transition <= 0) return IsNight(minuteOfDay, sunrise, sunset) ? 0f : 1f;
            float half = transition * 0.5f;
            float rise = Mathf.Clamp01((minuteOfDay - (sunrise - half)) / transition);
            float set = Mathf.Clamp01(((sunset + half) - minuteOfDay) / transition);
            return Mathf.Min(rise, set);
        }

        /// <summary>낮 동안 해의 진행(0=해 뜸, 1=해 짐). 밤이면 -1.</summary>
        public static float SunProgress(int minuteOfDay, int sunrise, int sunset)
        {
            if (IsNight(minuteOfDay, sunrise, sunset) || sunset <= sunrise) return -1f;
            return (minuteOfDay - sunrise) / (float)(sunset - sunrise);
        }

        /// <summary>밤 동안 달의 진행(0=해 진 직후, 1=해 뜨기 직전). 낮이면 -1.</summary>
        public static float MoonProgress(int minuteOfDay, int sunrise, int sunset)
        {
            if (!IsNight(minuteOfDay, sunrise, sunset)) return -1f;
            int nightLength = WorldClock.MinutesPerDay - (sunset - sunrise);
            if (nightLength <= 0) return -1f;
            int sinceSunset = minuteOfDay >= sunset ? minuteOfDay - sunset : minuteOfDay + WorldClock.MinutesPerDay - sunset;
            return sinceSunset / (float)nightLength;
        }

        /// <summary>진행(0~1)을 카메라 기준 호 위의 위치로. 양 끝은 baseHeight, 가운데는 peakHeight.</summary>
        public static Vector2 ArcPosition(float progress, float halfWidth, float baseHeight, float peakHeight)
        {
            float x = Mathf.Lerp(-halfWidth, halfWidth, progress);
            float y = baseHeight + (peakHeight - baseHeight) * Mathf.Sin(progress * Mathf.PI);
            return new Vector2(x, y);
        }
    }
}
