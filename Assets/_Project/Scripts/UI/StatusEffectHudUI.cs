using System.Collections.Generic;
using Game.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 상태 이상 아이콘 (docs/design/GAME_DESIGN.md 9.5). 걸리거나 풀릴 때만 켜고 끈다 — 매 프레임
    /// 다시 그리지 않는다.
    /// </summary>
    public sealed class StatusEffectHudUI : MonoBehaviour
    {
        [SerializeField] private PlayerStatusEffects statusEffects;
        [SerializeField] private StatusEffectDatabase database;
        [SerializeField] private RectTransform iconParent;
        [SerializeField] private GameObject iconTemplate;

        private readonly Dictionary<string, GameObject> iconsByEffectId = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (statusEffects == null) Debug.LogError("[StatusEffectHudUI] statusEffects 참조가 비어 있습니다.", this);
            if (database == null) Debug.LogError("[StatusEffectHudUI] database 참조가 비어 있습니다.", this);
            if (iconParent == null) Debug.LogError("[StatusEffectHudUI] iconParent 참조가 비어 있습니다.", this);
            if (iconTemplate == null) Debug.LogError("[StatusEffectHudUI] iconTemplate 참조가 비어 있습니다.", this);
            else iconTemplate.SetActive(false);

            BuildIcons();
        }

        private void OnEnable()
        {
            if (statusEffects != null)
            {
                statusEffects.Effects.EffectApplied += HandleApplied;
                statusEffects.Effects.EffectCleared += HandleCleared;
            }
            RefreshAll();
        }

        private void OnDisable()
        {
            if (statusEffects != null)
            {
                statusEffects.Effects.EffectApplied -= HandleApplied;
                statusEffects.Effects.EffectCleared -= HandleCleared;
            }
        }

        private void BuildIcons()
        {
            if (database == null || iconTemplate == null || iconParent == null) return;
            foreach (StatusEffectDefinition def in database.Effects)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;

                GameObject icon = Instantiate(iconTemplate, iconParent);
                Transform labelTransform = icon.transform.Find("Label");
                Image background = icon.GetComponent<Image>();
                Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;

                if (background != null) background.color = def.UiColor;
                if (label != null) label.text = def.DisplayName;
                icon.SetActive(false);
                iconsByEffectId[def.Id] = icon;
            }
        }

        private void RefreshAll()
        {
            if (statusEffects == null) return;
            foreach (KeyValuePair<string, GameObject> pair in iconsByEffectId)
                pair.Value.SetActive(statusEffects.Effects.Has(pair.Key));
        }

        private void HandleApplied(string id)
        {
            if (iconsByEffectId.TryGetValue(id, out GameObject icon)) icon.SetActive(true);
        }

        private void HandleCleared(string id)
        {
            if (iconsByEffectId.TryGetValue(id, out GameObject icon)) icon.SetActive(false);
        }
    }
}
