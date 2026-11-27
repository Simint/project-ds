using System;
using System.Collections.Generic;
using Game.Core;
using Game.Survival;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 늑대 후각 탐지 (docs/design/GAME_DESIGN.md 3.2, 4.3의 7단계 범위, ARCHITECTURE.md 4.2). R키(패드 오른쪽
    /// 스틱 누르기)로 쓴다. 손 모드와 상관없이 쓸 수 있고, 쓸 때마다 쿨타임이 돌고 허기가 준다. 반경 안의
    /// ScentSource 중 벽에 가로막히지 않은 것에 몇 초 동안 색깔 흔적(Effects 레이어)을 띄운다. 흔적은 대상을
    /// 따라간다(움직이는 적). 흔적 오브젝트는 Awake에서 미리 만들어 재사용한다(Object Pool과 같은 이유).
    /// </summary>
    public sealed class ScentSense : MonoBehaviour
    {
        // 발밑 피벗끼리 잇는 선은 땅을 스쳐 가로막힌 것으로 나오기 쉽다 — 몸통·대상 위쪽 높이에서 잰다(기술 값, 타일).
        private const float SightHeightTiles = 0.5f;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private ScentSettings settings;

        private readonly List<Marker> markers = new List<Marker>();
        private int[] selected = Array.Empty<int>();
        private IReadOnlyList<ScentSource> querySources;
        private Func<int, Vector2> positionAt;
        private Func<Vector2, Vector2, bool> isBlocked;
        private float cooldownRemaining;

        public float CooldownRemaining => cooldownRemaining;

        private sealed class Marker
        {
            public SpriteRenderer Renderer { get; set; }
            public ScentSource Target { get; set; }
            public float Remaining { get; set; }
        }

        private void Awake()
        {
            if (inputReader == null) Debug.LogError("[ScentSense] inputReader 참조가 비어 있습니다.", this);
            if (survival == null) Debug.LogError("[ScentSense] survival 참조가 비어 있습니다.", this);
            if (settings == null) Debug.LogError("[ScentSense] settings 참조가 비어 있습니다.", this);

            positionAt = i => querySources[i].Position + Vector2.up * SightHeightTiles;
            isBlocked = (from, to) => Physics2D.Linecast(from, to, GameLayers.SolidMask).collider != null;

            int count = settings != null ? settings.MaxMarkers : 0;
            selected = new int[count];
            for (int i = 0; i < count; i++) markers.Add(CreateMarker(i));
        }

        private Marker CreateMarker(int index)
        {
            var go = new GameObject($"ScentMarker{index}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = settings.MarkerSprite;
            GameLayers.Sorting.Apply(renderer, GameLayers.Sorting.Effects);
            go.SetActive(false);
            return new Marker { Renderer = renderer };
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.SniffPressed += HandleSniffPressed;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.SniffPressed -= HandleSniffPressed;
        }

        private void Update()
        {
            if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
        }

        private void LateUpdate()
        {
            float duration = settings != null ? settings.DisplaySeconds : 0f;
            for (int i = 0; i < markers.Count; i++)
            {
                Marker marker = markers[i];
                if (marker.Remaining <= 0f) continue;
                marker.Remaining -= Time.deltaTime;
                // 파괴됐거나(적·아이템) 꺼진(다 판 지점·털린 상자) 대상의 흔적은 바로 숨긴다.
                if (marker.Remaining <= 0f || marker.Target == null || !marker.Target.isActiveAndEnabled)
                {
                    marker.Remaining = 0f;
                    marker.Renderer.gameObject.SetActive(false);
                    continue;
                }
                marker.Renderer.transform.position = marker.Target.transform.position + Vector3.up * SightHeightTiles;
                Color color = marker.Renderer.color;
                color.a = duration > 0f ? Mathf.Clamp01(marker.Remaining / duration) : 0f;
                marker.Renderer.color = color;
            }
        }

        private void HandleSniffPressed()
        {
            if (settings == null || cooldownRemaining > 0f) return;
            ScentSystem system = GameServices.ScentSystem;
            if (system == null) return;

            cooldownRemaining = settings.CooldownSeconds;
            SurvivalStat hunger = survival != null ? survival.Stats.Get(StatIds.Hunger) : null;
            if (hunger != null && settings.HungerCost > 0f)
            {
                hunger.Add(-settings.HungerCost);
                survival.Stats.NotifyChanged();
            }

            querySources = system.Sources;
            Vector2 origin = (Vector2)transform.position + Vector2.up * SightHeightTiles;
            int found = ScentQuery.Select(origin, settings.RadiusTiles, querySources.Count, positionAt, isBlocked, selected);

            for (int i = 0; i < markers.Count; i++)
            {
                Marker marker = markers[i];
                if (i >= found)
                {
                    marker.Remaining = 0f;
                    marker.Renderer.gameObject.SetActive(false);
                    continue;
                }
                ScentSource source = querySources[selected[i]];
                marker.Target = source;
                marker.Remaining = settings.DisplaySeconds;
                marker.Renderer.color = settings.ColorFor(source.Kind);
                marker.Renderer.transform.position = source.transform.position + Vector3.up * SightHeightTiles;
                marker.Renderer.gameObject.SetActive(true);
            }
            querySources = null;
        }
    }
}
