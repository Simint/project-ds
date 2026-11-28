using System;
using Game.Core;
using Game.Inventory;
using Game.Maps;
using Game.Player;
using Game.Save;
using Game.Items;
using Game.World;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// PlayerSurvival.Died를 구독해 은신처에서 부활시킨다 (docs/design/GAME_DESIGN.md 5.8, ARCHITECTURE.md
    /// 4.8). 죽은 자리에 난이도가 정한 범위의 소지품을 사망 주머니로 남긴다. 맵 이동 도중에 죽으면 떠난 맵의
    /// 마지막 위치를 죽은 자리로 본다.
    /// </summary>
    public sealed class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private MapDefinition hubMap;
        [SerializeField] private string hubSpawnPointId = "";

        // 은신처로 돌려보내는 중인지. 그동안 또 죽으면(예: 이동 중 다시 HP 0) 주머니를 다시 만들지 않는다 —
        // 이미 비운 가방으로 새 주머니를 만들면 첫 주머니를 덮어써 소지품이 사라진다.
        private bool isReturningToHub;

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
        }

        private void HandleDied()
        {
            if (isReturningToHub)
            {
                survival.Revive();
                return;
            }
            DropDeathBag();
            survival.Revive();
            if (GameServices.MapLoader == null || hubMap == null) return;
            _ = ReturnToHub();
        }

        /// <summary>진행 중인 맵 이동이 있으면 끝나길 기다렸다가 은신처로 보낸다(이동 중에 부르면 Travel이 "이미
        /// 이동 중"으로 무시한다). 이동이 실패·취소돼도 표시는 반드시 풀어, 이후 사망이 무시되지 않게 한다.</summary>
        private async Awaitable ReturnToHub()
        {
            isReturningToHub = true;
            // 진행 중이던 이동이 끝나고 은신처 이동이 시작되기 전 틈(한 프레임)에 부활한 플레이어가 움직이거나
            // 아이템을 쓰지 않도록, 기다리는 동안 직접 입력을 잠근다.
            PlayerInputReader input = GameServices.PlayerInputReader;
            input?.SetLocked(this, true);
            try
            {
                MapLoader loader = GameServices.MapLoader;
                while (loader != null && loader.IsTraveling)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                if (loader != null) await loader.Travel(hubMap, hubSpawnPointId, 0);
            }
            catch (OperationCanceledException)
            {
                // 오브젝트가 파괴되는 중(게임 종료 등)이라 따로 알리지 않는다.
            }
            finally
            {
                if (input != null) input.SetLocked(this, false);
                isReturningToHub = false;
            }
        }

        /// <summary>죽은 자리(현재 맵·위치, 이동 중이면 떠난 맵의 마지막 위치)에 난이도가 정한 범위의 물건을
        /// 사망 주머니로 남기고 가방에서 뺀다 (5.8: 쉬움은 재료류만, 보통·어려움은 가방 전부). 새로 죽으면 이전
        /// 주머니는 덮어써 사라진다 (DeathBagRegistry.Set, 5.8 "마지막 하나만").</summary>
        private void DropDeathBag()
        {
            MapLoader loader = GameServices.MapLoader;
            DeathBagRegistry registry = GameServices.DeathBagRegistry;
            if (loader == null || registry == null || playerInventory == null) return;
            if (!loader.TryGetPlayerLocation(out MapDefinition map, out Vector2 position)) return;

            var record = new DeathBagRecord
            {
                mapId = map.Id,
                x = position.x,
                y = position.y
            };
            DeathDropScope scope = DifficultyRules.DropScope(GameServices.Difficulty);
            var dropped = new List<int>();
            foreach ((string itemId, int count, long spoilAtMinute, int durability, int slotIndex) in playerInventory.EnumerateSlots())
            {
                ItemDefinition def = playerInventory.FindDefinition(itemId);
                if (def != null && !DeathDropRules.ShouldDrop(def.Category, scope)) continue;
                dropped.Add(slotIndex);
                record.items.Add(new InventorySlotData
                {
                    itemId = itemId,
                    count = count,
                    spoilAtMinute = spoilAtMinute,
                    durability = durability,
                    slotIndex = slotIndex
                });
            }

            registry.Set(record);
            foreach (int slotIndex in dropped)
                playerInventory.Model.RemoveFromSlot(slotIndex, int.MaxValue);
        }
    }
}
