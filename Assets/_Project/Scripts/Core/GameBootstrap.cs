using System.Collections.Generic;
using Game.Inventory;
using Game.Maps;
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
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldClock worldClock;
        [SerializeField] private MapLoader mapLoader;
        [SerializeField] private TravelMenuUI travelMenu;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerSurvival playerSurvival;
        [SerializeField] private PlayerStatusEffects playerStatusEffects;
        [SerializeField] private MapDatabase mapDatabase;

        private void Awake()
        {
            if (worldClock == null) Debug.LogError("[GameBootstrap] worldClock 참조가 비어 있습니다.", this);
            if (mapLoader == null) Debug.LogError("[GameBootstrap] mapLoader 참조가 비어 있습니다.", this);
            if (travelMenu == null) Debug.LogError("[GameBootstrap] travelMenu 참조가 비어 있습니다.", this);
            if (playerInventory == null) Debug.LogError("[GameBootstrap] playerInventory 참조가 비어 있습니다.", this);
            if (playerSurvival == null) Debug.LogError("[GameBootstrap] playerSurvival 참조가 비어 있습니다.", this);
            if (playerStatusEffects == null) Debug.LogError("[GameBootstrap] playerStatusEffects 참조가 비어 있습니다.", this);
            if (mapDatabase == null) Debug.LogError("[GameBootstrap] mapDatabase 참조가 비어 있습니다.", this);

            GameServices.RegisterWorldClock(worldClock);
            GameServices.RegisterMapLoader(mapLoader);
            GameServices.RegisterTravelMenu(travelMenu);

            var mapStateRegistry = new MapStateRegistry();
            var saveService = new SaveService();
            GameServices.RegisterMapStateRegistry(mapStateRegistry);
            GameServices.RegisterSaveService(saveService);

            WorldSaveData save = saveService.Load();
            if (save != null) ApplySave(save, mapStateRegistry);

            GameServices.NotifyReady();
        }

        private void ApplySave(WorldSaveData save, MapStateRegistry mapStateRegistry)
        {
            worldClock.SetTotalMinutes(save.clockTotalMinutes);

            if (playerInventory != null)
            {
                var slots = new List<(string itemId, int count, long spoilAtMinute)>();
                foreach (InventorySlotData slot in save.inventorySlots)
                    slots.Add((slot.itemId, slot.count, slot.spoilAtMinute));
                playerInventory.RestoreSlots(slots);
            }

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
