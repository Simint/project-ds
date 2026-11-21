using UnityEngine;

namespace Game.Survival
{
    /// <summary>스탯 하나의 현재 값(상태). 정의의 min/max를 벗어나지 않는다.</summary>
    public sealed class SurvivalStat
    {
        public SurvivalStatDefinition Definition { get; }
        public float Value { get; private set; }

        public SurvivalStat(SurvivalStatDefinition definition, float startValue)
        {
            Definition = definition;
            Value = Mathf.Clamp(startValue, definition.Min, definition.Max);
        }

        public bool IsAtMin => Value <= Definition.Min;
        public bool IsAtMax => Value >= Definition.Max;

        public void SetValue(float value) => Value = Mathf.Clamp(value, Definition.Min, Definition.Max);

        public void Add(float delta) => SetValue(Value + delta);
    }
}
