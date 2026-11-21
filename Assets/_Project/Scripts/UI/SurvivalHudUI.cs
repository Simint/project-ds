using System.Collections.Generic;
using Game.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 왼쪽 위 생존 스탯 바 (docs/design/GAME_DESIGN.md 9.5). SurvivalStatDatabase 목록을 읽어 줄을
    /// 자동으로 만든다 — 스탯이 늘어도 이 코드를 고치지 않는다 (add-survival-stat 스킬 4절).
    /// </summary>
    public sealed class SurvivalHudUI : MonoBehaviour
    {
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private SurvivalStatDatabase database;
        [SerializeField] private RectTransform rowParent;
        [SerializeField] private GameObject rowTemplate;

        private readonly Dictionary<string, Image> fillsByStatId = new Dictionary<string, Image>();

        private void Awake()
        {
            if (survival == null) Debug.LogError("[SurvivalHudUI] survival 참조가 비어 있습니다.", this);
            if (database == null) Debug.LogError("[SurvivalHudUI] database 참조가 비어 있습니다.", this);
            if (rowParent == null) Debug.LogError("[SurvivalHudUI] rowParent 참조가 비어 있습니다.", this);
            if (rowTemplate == null) Debug.LogError("[SurvivalHudUI] rowTemplate 참조가 비어 있습니다.", this);
            else rowTemplate.SetActive(false);

            BuildRows();
        }

        private void OnEnable()
        {
            if (survival != null) survival.Stats.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (survival != null) survival.Stats.Changed -= Refresh;
        }

        private void BuildRows()
        {
            if (database == null || rowTemplate == null || rowParent == null) return;
            foreach (SurvivalStatDefinition def in database.Stats)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;

                GameObject row = Instantiate(rowTemplate, rowParent);
                row.SetActive(true);

                Transform fillTransform = row.transform.Find("Fill");
                Transform labelTransform = row.transform.Find("Label");
                Image fill = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
                Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;

                if (fill != null) fill.color = def.UiColor;
                if (label != null) label.text = def.DisplayName;
                if (fill != null) fillsByStatId[def.Id] = fill;
            }
        }

        private void Refresh()
        {
            if (survival == null) return;
            foreach (KeyValuePair<string, Image> pair in fillsByStatId)
            {
                SurvivalStat stat = survival.Stats.Get(pair.Key);
                if (stat == null) continue;
                float range = stat.Definition.Max - stat.Definition.Min;
                pair.Value.fillAmount = range > 0f ? (stat.Value - stat.Definition.Min) / range : 0f;
            }
        }
    }
}
