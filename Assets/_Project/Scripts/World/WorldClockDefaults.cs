using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 새 게임을 시작할 때 WorldClock이 쓰는 초기값 (ARCHITECTURE.md 4.8). 저장 파일이 있으면
    /// GameBootstrap이 덮어쓴다. 실제 값은 비공개 서브모듈 Assets/_Project/Data/에만 둔다
    /// (CLAUDE.md "비공개 서브모듈").
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/World Clock Defaults")]
    public sealed class WorldClockDefaults : ScriptableObject
    {
        [SerializeField] private long startMinutes = 0;
        [SerializeField] private string difficultyId = "";
        [SerializeField] private int seasonLengthDays = 0;
        [SerializeField] private float realSecondsPerDay = 0f;

        public long StartMinutes => startMinutes;
        public string DifficultyId => difficultyId;
        public int SeasonLengthDays => seasonLengthDays;
        public float RealSecondsPerDay => realSecondsPerDay;
    }
}
