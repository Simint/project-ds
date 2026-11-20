using System;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 월드를 만들 때 고르고 저장되는 상태 (ScriptableObject가 아니다, ARCHITECTURE.md 4.8).
    /// 기본값은 "현실 1초 = 게임 1.2분 (하루 1440분 = 현실 20분)" 기준이다.
    /// </summary>
    [Serializable]
    public sealed class WorldSettings
    {
        [SerializeField] private string difficultyId = "normal";
        [SerializeField] private int seasonLengthDays = 10;
        [SerializeField] private float realSecondsPerDay = 1200f;

        public string DifficultyId => difficultyId;
        public int SeasonLengthDays => seasonLengthDays;
        public float RealSecondsPerDay => realSecondsPerDay;
    }
}
