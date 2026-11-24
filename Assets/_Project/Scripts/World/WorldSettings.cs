using System;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 월드를 만들 때 고르고 저장되는 상태 (ScriptableObject가 아니다, ARCHITECTURE.md 4.8). 새로
    /// 시작할 때의 실제 값은 <see cref="WorldClockDefaults"/>(비공개 서브모듈 Data/)에서 가져온다.
    /// </summary>
    [Serializable]
    public sealed class WorldSettings
    {
        [SerializeField] private string difficultyId = "";
        [SerializeField] private int seasonLengthDays = 0;
        [SerializeField] private float realSecondsPerDay = 0f;

        public WorldSettings()
        {
        }

        public WorldSettings(string difficultyId, int seasonLengthDays, float realSecondsPerDay)
        {
            this.difficultyId = difficultyId;
            this.seasonLengthDays = seasonLengthDays;
            this.realSecondsPerDay = realSecondsPerDay;
        }

        public string DifficultyId => difficultyId;
        public int SeasonLengthDays => seasonLengthDays;
        public float RealSecondsPerDay => realSecondsPerDay;
    }
}
