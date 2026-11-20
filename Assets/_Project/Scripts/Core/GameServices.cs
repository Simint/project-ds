using System;
using Game.Maps;
using Game.Save;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 유일한 전역 접근 지점 (Service Locator). 매니저마다 싱글톤(XxxManager.Instance)을 만들지 않고
    /// 상주 씬(Boot)의 서비스를 여기에 등록해서 쓴다 (.claude/rules/csharp.md "의존성과 전역 접근").
    /// 등록은 <see cref="GameBootstrap"/>이 정해진 순서(시계 → 맵 → 나머지)로 한다.
    /// </summary>
    public static class GameServices
    {
        public static IWorldClock WorldClock { get; private set; }
        public static MapLoader MapLoader { get; private set; }
        public static TravelMenuUI TravelMenu { get; private set; }
        public static MapStateRegistry MapStateRegistry { get; private set; }
        public static SaveService SaveService { get; private set; }
        public static bool IsReady { get; private set; }

        /// <summary>모든 서비스 등록이 끝난 뒤 한 번 발행된다. 등록 순서와 무관하게 구독할 수 있다.</summary>
        public static event Action Ready;

        public static void RegisterWorldClock(IWorldClock clock) => WorldClock = clock;
        public static void RegisterMapLoader(MapLoader loader) => MapLoader = loader;
        public static void RegisterTravelMenu(TravelMenuUI menu) => TravelMenu = menu;
        public static void RegisterMapStateRegistry(MapStateRegistry registry) => MapStateRegistry = registry;
        public static void RegisterSaveService(SaveService service) => SaveService = service;

        public static void NotifyReady()
        {
            IsReady = true;
            Ready?.Invoke();
        }

        /// <summary>
        /// Enter Play Mode Settings에서 도메인 리로드를 끄면 정적 필드가 이전 Play Mode 세션 값 그대로
        /// 남는다. Play Mode를 시작할 때마다(도메인 리로드 여부와 무관하게) 호출되므로 여기서 초기화한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            WorldClock = null;
            MapLoader = null;
            TravelMenu = null;
            MapStateRegistry = null;
            SaveService = null;
            IsReady = false;
            Ready = null;
        }
    }
}
