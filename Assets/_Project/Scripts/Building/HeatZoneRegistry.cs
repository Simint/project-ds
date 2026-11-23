using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// 켜진 화로처럼 "근처에 있으면 체온이 더 빨리 회복되는" 영역의 목록 (ARCHITECTURE.md 2장 원칙 9
    /// "물리 쿼리보다 등록부", 4.1 EnvironmentZone과 같은 생각). 화로는 놓일 때 한 번만 등록하고, 지금
    /// 켜져 있는지는 isActive로 그때그때 물어본다 — 불을 끌 때마다 따로 등록 해제를 부를 필요가 없다.
    /// PlayerSurvival이 분마다 플레이어 위치가 들어 있는 영역을 훑어 합산한다.
    /// </summary>
    public sealed class HeatZoneRegistry
    {
        private readonly struct Zone
        {
            public readonly Transform Source;
            public readonly float Radius;
            public readonly float ExtraRecoveryPerHour;
            public readonly Func<bool> IsActive;

            public Zone(Transform source, float radius, float extraRecoveryPerHour, Func<bool> isActive)
            {
                Source = source;
                Radius = radius;
                ExtraRecoveryPerHour = extraRecoveryPerHour;
                IsActive = isActive;
            }
        }

        private readonly List<Zone> zones = new List<Zone>();

        public void Register(Transform source, float radius, float extraRecoveryPerHour, Func<bool> isActive) =>
            zones.Add(new Zone(source, radius, extraRecoveryPerHour, isActive));

        public void Unregister(Transform source) => zones.RemoveAll(zone => zone.Source == source);

        public float GetExtraRecoveryAt(Vector2 position)
        {
            float total = 0f;
            foreach (Zone zone in zones)
            {
                if (zone.Source == null || !zone.IsActive()) continue;
                if (Vector2.Distance(zone.Source.position, position) <= zone.Radius) total += zone.ExtraRecoveryPerHour;
            }
            return total;
        }
    }
}
