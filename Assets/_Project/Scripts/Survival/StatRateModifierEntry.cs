using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// 상태 이상이 거는 스탯 모디파이어 하나 (예: 질병 → 허기·갈증 감소 배율 증가). statId는
    /// SurvivalStatDefinition.id와 맞춘다.
    /// </summary>
    [System.Serializable]
    public struct StatRateModifierEntry
    {
        [SerializeField] private string statId;
        [SerializeField] private float multiplier;
        [SerializeField] private float additivePerHour;

        public string StatId => statId;
        public float Multiplier => multiplier;
        public float AdditivePerHour => additivePerHour;
    }
}
