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
    /// Boot 씬에 하나 둔다. 서비스를 정해진 순서(시계 → 맵 → 나머지)로 GameServices에 등록하고,
    /// 저장 파일이 있으면 불러와 시계·가방·맵 상태·시작 맵을 그 내용으로 덮어쓴 뒤 준비 완료를
    /// 알린다 (ARCHITECTURE.md 2.3, 3.1, 4.10).
    /// </summary>
    // WorldClock은 -100이라 항상 먼저 Awake가 돈다. GameBootstrap은 그 다음, 나머지(기본값 0) 플레이어
    // 쪽 컴포넌트보다는 먼저 돌도록 둬서, 여기서 복원하는 값을 플레이어 쪽 컴포넌트가 자기 Awake에서
    // 실수로 덮어쓸 여지를 없앤다(지금은 아무도 그렇게 하지 않지만, 순서를 명시해 두면 나중에 생기는
    // 사고를 막는다).
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

            GameServices.RegisterWorldClock(worldClock);
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
            var noiseSystem = new NoiseSystem();
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

            WorldSaveData save = saveService.Load();
            if (save != null) ApplySave(save, mapStateRegistry, hubStructureRegistry, deathBagRegistry);

            GameServices.NotifyReady();
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
