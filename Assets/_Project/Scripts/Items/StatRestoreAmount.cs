using UnityEngine;

namespace Game.Items
{
    /// <summary>음식·물이 회복시키는 생존 스탯 하나. statId는 Game.Survival.StatIds와 맞춘다(문자열로만 참조해
    /// Items가 Survival을 몰라도 되게 한다).</summary>
    [System.Serializable]
    public struct StatRestoreAmount
    {
        [SerializeField] private string statId;
        [SerializeField] private float amount;

        public string StatId => statId;
        public float Amount => amount;
    }
}
