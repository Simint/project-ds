using System;
using Game.Building;
using Game.Combat;
using Game.Crafting;
using Game.Inventory;
using Game.Lore;
using Game.Maps;
using Game.Player;
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
        public static PlayerInventory PlayerInventory { get; private set; }
        public static PlayerInputReader PlayerInputReader { get; private set; }
        public static MapStateRegistry MapStateRegistry { get; private set; }
        public static SaveService SaveService { get; private set; }
        public static StorageRegistry StorageRegistry { get; private set; }
        public static HubStructureRegistry HubStructureRegistry { get; private set; }
        public static HeatZoneRegistry HeatZoneRegistry { get; private set; }
        public static NoiseSystem NoiseSystem { get; private set; }
        public static DeathBagRegistry DeathBagRegistry { get; private set; }
        public static ScentSystem ScentSystem { get; private set; }
        public static LoreCollection LoreCollection { get; private set; }
        public static LoreNotesUI LoreNotesUI { get; private set; }
        public static BuildModeController BuildModeController { get; private set; }
        public static BuildGrid BuildGrid { get; private set; }
        public static StorageUI StorageUI { get; private set; }
        public static CraftingUI CraftingUI { get; private set; }
        public static SleepUI SleepUI { get; private set; }
        public static bool IsReady { get; private set; }

        /// <summary>모든 서비스 등록이 끝난 뒤 한 번 발행된다. 등록 순서와 무관하게 구독할 수 있다.</summary>
        public static event Action Ready;

        public static void RegisterWorldClock(IWorldClock clock) => WorldClock = clock;
        public static void RegisterMapLoader(MapLoader loader) => MapLoader = loader;
        public static void RegisterTravelMenu(TravelMenuUI menu) => TravelMenu = menu;

        /// <summary>은신처 씬 등 다른 씬의 오브젝트가 플레이어를 계속(한 번이 아니라) 참조해야 할 때 쓴다
        /// (예: BuildModeController). 대부분의 상호작용은 이것 대신 interactor 매개변수로 받는다.</summary>
        public static void RegisterPlayer(PlayerInventory inventory, PlayerInputReader inputReader)
        {
            PlayerInventory = inventory;
            PlayerInputReader = inputReader;
        }
        public static void RegisterMapStateRegistry(MapStateRegistry registry) => MapStateRegistry = registry;
        public static void RegisterSaveService(SaveService service) => SaveService = service;
        public static void RegisterStorageRegistry(StorageRegistry registry) => StorageRegistry = registry;
        public static void RegisterHubStructureRegistry(HubStructureRegistry registry) => HubStructureRegistry = registry;
        public static void RegisterHeatZoneRegistry(HeatZoneRegistry registry) => HeatZoneRegistry = registry;
        public static void RegisterNoiseSystem(NoiseSystem system) => NoiseSystem = system;
        public static void RegisterDeathBagRegistry(DeathBagRegistry registry) => DeathBagRegistry = registry;
        public static void RegisterScentSystem(ScentSystem system) => ScentSystem = system;
        public static void RegisterLoreCollection(LoreCollection collection) => LoreCollection = collection;
        public static void RegisterLoreNotesUI(LoreNotesUI ui) => LoreNotesUI = ui;

        /// <summary>은신처 씬에만 있는 컴포넌트라, 그 씬이 열려 있을 때만 OnEnable/OnDisable로 등록·해제한다
        /// (다른 서비스들과 달리 Boot에서 한 번만 등록하지 않는다).</summary>
        public static void RegisterBuildModeController(BuildModeController controller) => BuildModeController = controller;

        /// <summary>BuildModeController와 같은 이유로 은신처 씬이 열려 있을 때만 등록·해제한다.</summary>
        public static void RegisterBuildGrid(BuildGrid grid) => BuildGrid = grid;

        public static void RegisterStorageUI(StorageUI ui) => StorageUI = ui;
        public static void RegisterCraftingUI(CraftingUI ui) => CraftingUI = ui;
        public static void RegisterSleepUI(SleepUI ui) => SleepUI = ui;

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
            PlayerInventory = null;
            PlayerInputReader = null;
            MapStateRegistry = null;
            SaveService = null;
            StorageRegistry = null;
            HubStructureRegistry = null;
            HeatZoneRegistry = null;
            NoiseSystem = null;
            DeathBagRegistry = null;
            ScentSystem = null;
            LoreCollection = null;
            LoreNotesUI = null;
            BuildModeController = null;
            BuildGrid = null;
            StorageUI = null;
            CraftingUI = null;
            SleepUI = null;
            IsReady = false;
            Ready = null;
        }
    }
}
