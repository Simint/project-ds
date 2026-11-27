using System.Collections.Generic;

namespace Game.World
{
    /// <summary>
    /// 날씨 뽑기와 효과 계산 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 실내에서는 날씨의 체온·시야·소음 효과가
    /// 없다(계절 기온은 따로 적용된다). 순수 함수라 EditMode 테스트로 검증한다.
    /// </summary>
    public static class WeatherRules
    {
        /// <summary>계절 가중치로 날씨 하나를 고른다. roll01은 [0, 1). 뽑을 것이 없으면 null.</summary>
        public static WeatherDefinition Pick(IReadOnlyList<WeatherChance> chances, Season season, double roll01)
        {
            if (chances == null) return null;
            double total = 0;
            for (int i = 0; i < chances.Count; i++)
                if (chances[i]?.Weather != null) total += System.Math.Max(0f, chances[i].WeightFor(season));
            if (total <= 0) return null;

            double target = roll01 * total;
            double running = 0;
            WeatherDefinition last = null;
            for (int i = 0; i < chances.Count; i++)
            {
                WeatherChance c = chances[i];
                if (c?.Weather == null) continue;
                float weight = System.Math.Max(0f, c.WeightFor(season));
                if (weight <= 0f) continue;
                last = c.Weather;
                running += weight;
                if (target < running) return c.Weather;
            }
            return last;
        }

        /// <summary>지금 날씨가 그 계절에 나올 가중치. 목록에 없으면 0.</summary>
        public static float WeightOf(IReadOnlyList<WeatherChance> chances, WeatherDefinition weather, Season season)
        {
            if (chances == null || weather == null) return 0f;
            for (int i = 0; i < chances.Count; i++)
                if (chances[i]?.Weather == weather) return System.Math.Max(0f, chances[i].WeightFor(season));
            return 0f;
        }

        /// <summary>화면 덮개의 짙기(알파). 덮개 색의 알파와 '플레이어 시야 배율'로 줄어든 만큼(1 - 배율) 중 큰 쪽. 실내면 0.</summary>
        public static float OverlayAlpha(WeatherDefinition weather, bool indoor)
        {
            if (weather == null || indoor) return 0f;
            float fromSight = 1f - UnityEngine.Mathf.Clamp01(weather.PlayerSightMultiplier);
            return UnityEngine.Mathf.Clamp01(System.Math.Max(weather.OverlayColor.a, fromSight));
        }

        /// <summary>최소~최대 지속 시간 사이(포함). 최소 1분(0분이면 매 분 다시 뽑게 되므로).</summary>
        public static int Duration(WeatherDefinition weather, double roll01)
        {
            if (weather == null) return 1;
            int min = System.Math.Max(1, weather.MinDurationMinutes);
            int max = System.Math.Max(min, weather.MaxDurationMinutes);
            int value = min + (int)System.Math.Floor(roll01 * (max - min + 1));
            return value > max ? max : value;
        }

        public static float TemperaturePerHour(WeatherDefinition weather, bool indoor) =>
            weather == null || indoor ? 0f : weather.TemperaturePerHour;

        public static float PlayerSightMultiplier(WeatherDefinition weather, bool indoor) =>
            weather == null || indoor ? 1f : weather.PlayerSightMultiplier;

        public static float EnemySightMultiplier(WeatherDefinition weather, bool indoor) =>
            weather == null || indoor ? 1f : weather.EnemySightMultiplier;

        public static float NoiseRadiusMultiplier(WeatherDefinition weather, bool indoor) =>
            weather == null || indoor ? 1f : weather.NoiseRadiusMultiplier;
    }
}
