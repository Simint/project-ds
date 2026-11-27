using System.Collections.Generic;
using Game.Building;
using Game.Combat;
using Game.Crafting;
using Game.Inventory;
using Game.Lore;
using Game.Maps;
using Game.Player;
using Game.Save;
using Game.Survival;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Boot 씬에 하나 둔다. 서비스를 정해진 순서(시계 → 계절 → 날씨 → 낮밤 → 맵·저장 → 나머지)로 GameServices에
    /// 등록하고 준비 완료를 알린다. 저장 파일은 Awake에서 읽어 두기만 하고, 타이틀에서 '이어하기'를 고르면 그때
    /// 시계·가방·맵 상태·시작 맵·날씨를 덮어쓴다(ContinueGame). '새 게임'이면 저장을 지우고 새 설정으로 시작한다
    /// (ARCHITECTURE.md 2.3, 3.1, 4.8, 4.10). 복원이 플레이어 쪽 OnEnable보다 늦으므로, 복원 대상은 불러올 때
    /// 스스로 다시 반영해야 한다(예: PlayerStatusEffects.RestoreEffects).
    /// </summary>
    // WorldClock은 -100이라 항상 먼저 Awake가 돈다. GameBootstrap은 그 다음, 나머지(기본값 0)보다 먼저 돌아서
    // 서비스 등록이 다른 컴포넌트의 Awake·OnEnable보다 앞서게 한다.
    [DefaultExecutionOrder(-50)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private MapLoader mapLoader;
        [SerializeField] private TravelMenuUI travelMenu;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerInputReader playerInputReader;
        [SerializeField] private PlayerSurvival playerSurvival;
        [SerializeField] private PlayerStatusEffects playerStatusEffects;
        [SerializeField] private StorageUI storageUI;
        [SerializeField] private CraftingUI craftingUI;
        [SerializeField] private SleepUI sleepUI;
        [SerializeField] private LoreNotesUI loreNotesUI;
        [SerializeField] private MapDatabase mapDatabase;
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private SeasonSettings seasonSettings;
        [SerializeField] private WeatherSettings weatherSettings;
        [SerializeField] private DifficultyDatabase difficultyDatabase;

        private WorldSaveData pendingSave;
        private SeasonSystem seasonSystem;
        private WeatherSystem weatherSystem;
        private bool started;

        /// <summary>이어할 저장이 있는지 (타이틀의 '이어하기' 버튼).</summary>
        public bool HasSave => pendingSave != null;
        public DifficultyDatabase Difficulties => difficultyDatabase;

        private void Awake()
        {
            if (worldClock == null) Debug.LogError("[GameBootstrap] worldClock 참조가 비어 있습니다.", this);
            if (mapLoader == null) Debug.LogError("[GameBootstrap] mapLoader 참조가 비어 있습니다.", this);
            if (travelMenu == null) Debug.LogError("[GameBootstrap] travelMenu 참조가 비어 있습니다.", this);
            if (playerInventory == null) Debug.LogError("[GameBootstrap] playerInventory 참조가 비어 있습니다.", this);
            if (playerInputReader == null) Debug.LogError("[GameBootstrap] playerInputReader 참조가 비어 있습니다.", this);
            if (playerSurvival == null) Debug.LogError("[GameBootstrap] playerSurvival 참조가 비어 있습니다.", this);
            if (playerStatusEffects == null) Debug.LogError("[GameBootstrap] playerStatusEffects 참조가 비어 있습니다.", this);
            if (storageUI == null) Debug.LogError("[GameBootstrap] storageUI 참조가 비어 있습니다.", this);
            if (craftingUI == null) Debug.LogError("[GameBootstrap] craftingUI 참조가 비어 있습니다.", this);
            if (sleepUI == null) Debug.LogError("[GameBootstrap] sleepUI 참조가 비어 있습니다.", this);
            if (loreNotesUI == null) Debug.LogError("[GameBootstrap] loreNotesUI 참조가 비어 있습니다.", this);
            if (mapDatabase == null) Debug.LogError("[GameBootstrap] mapDatabase 참조가 비어 있습니다.", this);
            if (dayNightCycle == null) Debug.LogError("[GameBootstrap] dayNightCycle 참조가 비어 있습니다.", this);
            if (seasonSettings == null) Debug.LogError("[GameBootstrap] seasonSettings 참조가 비어 있습니다.", this);
            if (weatherSettings == null) Debug.LogError("[GameBootstrap] weatherSettings 참조가 비어 있습니다.", this);
            if (difficultyDatabase == null) Debug.LogError("[GameBootstrap] difficultyDatabase 참조가 비어 있습니다.", this);

            // 등록 순서: 시계 → 계절 → 날씨 → (낮밤) → 맵·저장 → 나머지 (.claude/rules/csharp.md).
            GameServices.RegisterWorldClock(worldClock);
            seasonSystem = new SeasonSystem(worldClock, seasonSettings);
            GameServices.RegisterSeasons(seasonSystem);
            weatherSystem = new WeatherSystem(worldClock, seasonSystem, weatherSettings, System.Environment.TickCount);
            GameServices.RegisterWeather(weatherSystem);
            GameServices.RegisterDayNight(dayNightCycle);
            GameServices.RegisterMapLoader(mapLoader);
            GameServices.RegisterTravelMenu(travelMenu);
            GameServices.RegisterPlayer(playerInventory, playerInputReader);
            GameServices.RegisterStorageUI(storageUI);
            GameServices.RegisterCraftingUI(craftingUI);
            GameServices.RegisterSleepUI(sleepUI);
            GameServices.RegisterLoreNotesUI(loreNotesUI);

            var mapStateRegistry = new MapStateRegistry();
            var saveService = new SaveService();
            var storageRegistry = new StorageRegistry();
            var hubStructureRegistry = new HubStructureRegistry();
            var heatZoneRegistry = new HeatZoneRegistry();
            var noiseSystem = new NoiseSystem(() => WeatherRules.NoiseRadiusMultiplier(GameServices.Weather?.Current, GameServices.IsIndoor));
            var deathBagRegistry = new DeathBagRegistry();
            var scentSystem = new ScentSystem();
            var loreCollection = new LoreCollection();
            GameServices.RegisterMapStateRegistry(mapStateRegistry);
            GameServices.RegisterSaveService(saveService);
            GameServices.RegisterStorageRegistry(storageRegistry);
            GameServices.RegisterHubStructureRegistry(hubStructureRegistry);
            GameServices.RegisterHeatZoneRegistry(heatZoneRegistry);
            GameServices.RegisterNoiseSystem(noiseSystem);
            GameServices.RegisterDeathBagRegistry(deathBagRegistry);
            GameServices.RegisterScentSystem(scentSystem);
            GameServices.RegisterLoreCollection(loreCollection);

            // 타이틀에서 고르기 전에는 시간이 흐르지 않고, 땅이 없는 Boot 씬에서 플레이어가 떨어지지 않게 멈춰 둔다.
            pendingSave = saveService.Load();
            if (worldClock != null) worldClock.IsPaused = true;
            Rigidbody2D playerBody = playerInventory != null ? playerInventory.GetComponent<Rigidbody2D>() : null;
            if (playerBody != null) playerBody.simulated = false;

            GameServices.NotifyReady();
        }

        private void OnDestroy()
        {
            weatherSystem?.Dispose();
            seasonSystem?.Dispose();
        }

        /// <summary>타이틀 '이어하기'. 저장이 없으면 기본 설정으로 새 게임을 시작한다.</summary>
        public void ContinueGame()
        {
            if (started) return;
            if (pendingSave == null)
            {
                StartNewGame(difficultyDatabase != null ? difficultyDatabase.Default : null, 0);
                return;
            }
            WorldSaveData save = pendingSave;
            ApplySave(save, GameServices.MapStateRegistry, GameServices.HubStructureRegistry, GameServices.DeathBagRegistry);
            if (save.settings != null) worldClock.ApplySettings(save.settings);
            GameServices.SetDifficulty(difficultyDatabase != null ? difficultyDatabase.Find(worldClock.Settings.DifficultyId) : null);
            seasonSystem.Refresh();
            weatherSystem.Restore(save.weatherId, save.weatherEndsAtMinute, save.rainMinutesTotal);
            StartPlaying();
        }

        /// <summary>타이틀 '새 게임'. 기존 저장은 지운다(확인은 타이틀이 받는다). seasonLengthDays가 0이면 기본값.</summary>
        public void StartNewGame(DifficultyDefinition difficulty, int seasonLengthDays)
        {
            if (started) return;
            GameServices.SaveService?.Delete();
            pendingSave = null;
            string difficultyId = difficulty != null ? difficulty.Id : worldClock.Settings.DifficultyId;
            worldClock.ApplySettings(new WorldSettings(difficultyId, seasonLengthDays, 0f));
            worldClock.SetTotalMinutes(worldClock.DefaultStartMinutes);
            GameServices.SetDifficulty(difficulty != null ? difficulty : difficultyDatabase != null ? difficultyDatabase.Find(difficultyId) : null);
            seasonSystem.Refresh();
            weatherSystem.Begin();
            StartPlaying();
        }

        private void StartPlaying()
        {
            started = true;
            if (dayNightCycle != null) dayNightCycle.Refresh();
            worldClock.IsPaused = false;
            mapLoader.BeginGame();
            GameServices.NotifyGameStarted();
        }

        private void ApplySave(WorldSaveData save, MapStateRegistry mapStateRegistry, HubStructureRegistry hubStructureRegistry, DeathBagRegistry deathBagRegistry)
        {
            worldClock.SetTotalMinutes(save.clockTotalMinutes);

            if (playerInventory != null)
            {
                var slots = new List<(string itemId, int count, long spoilAtMinute, int durability, int slotIndex)>();
                foreach (InventorySlotData slot in save.inventorySlots)
                    slots.Add((slot.itemId, slot.count, slot.spoilAtMinute, slot.durability, slot.slotIndex));
                playerInventory.RestoreSlots(slots);
            }

            hubStructureRegistry.SetCurrent(save.hubStructures);
            deathBagRegistry.SetFromSave(save.deathBag);
            GameServices.LoreCollection?.SetFromSave(save.loreNoteIds);

            if (playerSurvival != null)
            {
                var values = new List<(string id, float value)>();
                foreach (StatSaveData stat in save.stats)
                    values.Add((stat.id, stat.value));
                playerSurvival.RestoreStats(values);
            }

            if (playerStatusEffects != null)
            {
                var effects = new List<(string id, long untilMinute)>();
                foreach (StatusEffectSaveData effect in save.statusEffects)
                    effects.Add((effect.id, effect.untilMinute));
                playerStatusEffects.RestoreEffects(effects);
            }

            foreach (MapSaveData mapData in save.maps)
            {
                var objects = new List<KeyValuePair<string, ObjectStateRecord>>();
                foreach (ObjectSaveData obj in mapData.objects)
                    objects.Add(new KeyValuePair<string, ObjectStateRecord>(
                        obj.persistentId, new ObjectStateRecord(obj.state, obj.untilMinute, obj.extra)));
                mapStateRegistry.SetMapData(mapData.mapId, mapData.lastVisitedMinute, objects);
            }

            MapDefinition savedMap = mapDatabase != null ? mapDatabase.Find(save.currentMapId) : null;
            if (savedMap != null) mapLoader.SetStartOverride(savedMap, new Vector2(save.playerX, save.playerY));
            else GameLog.Warning("GameBootstrap", $"저장된 맵 id '{save.currentMapId}'를 찾지 못해 기본 시작 맵을 씁니다.", this);
        }
    }
}
