using Game.Core;
using Game.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace Game.World
{
    /// <summary>
    /// 지금 날씨를 화면과 소음에 반영한다 (docs/design/GAME_DESIGN.md 5.6의 8단계 범위). 계산은 WeatherRules가 맡고 여기는
    /// 표시만 한다.
    /// - 안개 등: 화면 덮개(overlayColor)로 플레이어 시야를 줄인다.
    /// - 비·폭우·눈: 카메라를 따라다니는 파티클을 켠다(폭우는 더 많이).
    /// 폭우 소음 감쇠는 규칙이라 여기가 아니라 NoiseSystem이 WeatherRules로 직접 계산한다.
    /// 실내에서는 효과를 모두 끈다(창밖 모습·소리는 실내 그림·소리가 생기면 넣는다).
    /// </summary>
    public sealed class WeatherPresenter : MonoBehaviour
    {
        [SerializeField] private Image overlay;
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [Tooltip("비와 폭우의 빗줄기 양(초당 입자 수, 표시용).")]
        [SerializeField] [Min(0f)] private float rainEmission = 0f;
        [SerializeField] [Min(0f)] private float stormEmission = 0f;

        private WeatherSystem weather;
        private MapLoader mapLoader;

        private void Awake()
        {
            if (overlay == null) Debug.LogError("[WeatherPresenter] overlay 참조가 비어 있습니다.", this);
            if (rainParticles == null) Debug.LogError("[WeatherPresenter] rainParticles 참조가 비어 있습니다.", this);
            if (snowParticles == null) Debug.LogError("[WeatherPresenter] snowParticles 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            GameServices.Ready += HandleReady;
            GameServices.GameStarted += Refresh;
            if (GameServices.IsReady) HandleReady();
        }

        private void OnDisable()
        {
            GameServices.Ready -= HandleReady;
            GameServices.GameStarted -= Refresh;
            if (weather != null) weather.WeatherChanged -= HandleWeatherChanged;
            if (mapLoader != null) mapLoader.MapChanged -= HandleMapChanged;
            weather = null;
            mapLoader = null;
        }

        private void HandleReady()
        {
            if (weather != null) return;
            weather = GameServices.Weather;
            mapLoader = GameServices.MapLoader;
            if (weather != null) weather.WeatherChanged += HandleWeatherChanged;
            if (mapLoader != null) mapLoader.MapChanged += HandleMapChanged;
            Refresh();
        }

        private void HandleWeatherChanged(WeatherDefinition _) => Refresh();

        private void HandleMapChanged(MapDefinition _) => Refresh();

        public void Refresh()
        {
            WeatherDefinition current = weather?.Current;
            bool indoor = GameServices.IsIndoor;
            bool outdoorWeather = current != null && !indoor && GameServices.IsGameStarted;

            if (overlay != null)
            {
                Color color = outdoorWeather ? current.OverlayColor : Color.clear;
                color.a = outdoorWeather ? WeatherRules.OverlayAlpha(current, indoor) : 0f;
                overlay.color = color;
                overlay.enabled = color.a > 0f;
            }

            WeatherKind kind = outdoorWeather ? current.Kind : WeatherKind.Clear;
            SetParticles(rainParticles, kind == WeatherKind.Rain || kind == WeatherKind.Storm, kind == WeatherKind.Storm ? stormEmission : rainEmission);
            SetParticles(snowParticles, kind == WeatherKind.Snow, -1f);

        }

        private static void SetParticles(ParticleSystem particles, bool on, float emission)
        {
            if (particles == null) return;
            if (emission >= 0f)
            {
                ParticleSystem.EmissionModule module = particles.emission;
                module.rateOverTime = emission;
            }
            if (on && !particles.isPlaying) particles.Play();
            else if (!on && particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
