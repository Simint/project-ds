using Game.Core;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 생존 스탯 하나의 정의 (ARCHITECTURE.md 4.3, add-survival-stat 스킬). changePerGameHour는 분당이
    /// 아니라 게임 내 1시간 기준 기본 변화량(음수=감소)이고, 손 모드·환경 모디파이어가 여기 곱·더해진다
    /// (SurvivalStats.SetModifier). HP·스태미나는 자체 changePerGameHour를 쓰지 않고(0으로 둔다)
    /// PlayerSurvival이 각자의 규칙(허기·갈증·체온 상태, 달리기 여부)으로 직접 갱신한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Survival/Survival Stat Definition")]
    public sealed class SurvivalStatDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private float min = 0f;
        [SerializeField] private float max = 100f;
        [SerializeField] private float startValue = 100f;
        [SerializeField] private float changePerGameHour = 0f;
        [SerializeField] private Color uiColor = Color.white;

        public string Id => id;
        public string DisplayName => displayName;
        public float Min => min;
        public float Max => max;
        public float StartValue => startValue;
        public float ChangePerGameHour => changePerGameHour;
        public Color UiColor => uiColor;
    }
}
