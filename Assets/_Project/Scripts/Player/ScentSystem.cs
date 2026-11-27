using System.Collections.Generic;

namespace Game.Player
{
    /// <summary>
    /// 후각 대상 등록부 (ARCHITECTURE.md 2장 원칙 9, 4.2). 물리 레이어 대신 ScentSource가 OnEnable/OnDisable로
    /// 들어오고 나간다. NoiseSystem과 같은 이유로 순수 C# 클래스다.
    /// </summary>
    public sealed class ScentSystem
    {
        private readonly List<ScentSource> sources = new List<ScentSource>();

        public IReadOnlyList<ScentSource> Sources => sources;

        public void Register(ScentSource source)
        {
            if (source != null && !sources.Contains(source)) sources.Add(source);
        }

        public void Unregister(ScentSource source) => sources.Remove(source);
    }
}
