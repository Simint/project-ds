using Game.Core;
using Game.Inventory;
using Game.Maps;
using Game.Save;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// PlayerSurvival.Died를 구독해 은신처에서 부활시킨다 (docs/design/GAME_DESIGN.md 5.8, ARCHITECTURE.md
    /// 4.8). 죽은 자리에 소지품을 통째로 사망 주머니로 남긴다 — 난이도가 들어가는 8단계 전까지는 "보통"
    /// 기준(가방 안 물건 전부)으로 떨어뜨린다(장비 슬롯은 아직 없어 해당 사항 없음, 6.5).
    /// </summary>
    public sealed class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private MapDefinition hubMap;
        [SerializeField] private string hubSpawnPointId = "";

        private void Awake()
        {
            if (survival == null) Debug.LogError("[PlayerDeath] survival 참조가 비어 있습니다.", this);
            if (playerInventory == null) Debug.LogError("[PlayerDeath] playerInventory 참조가 비어 있습니다.", this);
            if (hubMap == null) Debug.LogError("[PlayerDeath] hubMap 참조가 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (survival != null) survival.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (survival != null) survival.Died -= HandleDied;
            if (GameServices.MapLoader != null) GameServices.MapLoader.MapChanged -= HandleMapChangedAfterDeath;
        }

        private void HandleDied()
        {
            DropDeathBag();
            survival.Revive();
            MapLoader loader = GameServices.MapLoader;
            if (loader == null || hubMap == null) return;

            // 맵 이동 도중(페이드·씬 로드 중)에 죽으면 Travel이 "이미 이동 중"이라며 조용히 무시한다 —
            // 지금 진행 중인 이동이 끝날 때까지 기다렸다가 은신처로 되돌린다.
            if (loader.IsTraveling)
                loader.MapChanged += HandleMapChangedAfterDeath;
            else
                _ = loader.Travel(hubMap, hubSpawnPointId, 0);
        }

        private void HandleMapChangedAfterDeath(MapDefinition arrivedAt)
        {
            MapLoader loader = GameServices.MapLoader;
            if (loader == null) return;
            loader.MapChanged -= HandleMapChangedAfterDeath;
            if (hubMap != null) _ = loader.Travel(hubMap, hubSpawnPointId, 0);
        }

        /// <summary>죽은 자리(현재 맵·위치)와 가방 전체를 사망 주머니로 남기고 가방을 비운다. 새로
        /// 죽으면 이전 주머니는 덮어써 사라진다(DeathBagRegistry.Set, 5.8 "마지막 하나만").</summary>
        private void DropDeathBag()
        {
            MapLoader loader = GameServices.MapLoader;
            DeathBagRegistry registry = GameServices.DeathBagRegistry;
            if (loader == null || loader.CurrentMap == null || registry == null || playerInventory == null) return;

            var record = new DeathBagRecord
            {
                mapId = loader.CurrentMap.Id,
                x = transform.position.x,
                y = transform.position.y
            };
            foreach ((string itemId, int count, long spoilAtMinute, int durability, int slotIndex) in playerInventory.EnumerateSlots())
                record.items.Add(new InventorySlotData
                {
                    itemId = itemId,
                    count = count,
                    spoilAtMinute = spoilAtMinute,
                    durability = durability,
                    slotIndex = slotIndex
                });

            registry.Set(record);
            playerInventory.Model.Clear();
        }
    }
}
