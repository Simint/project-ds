using System.Collections.Generic;

namespace Game.Crafting
{
    /// <summary>
    /// 은신처에 설치된 모든 재료 공급원(보관함 등)의 목록. 각 공급원이 OnEnable/OnDisable에서
    /// 등록·해제한다 (ARCHITECTURE.md 2장 원칙 9 "물리 쿼리보다 등록부"). MaterialSourceAggregator가
    /// 이 목록 + 가방을 하나로 묶어서 쓴다.
    /// </summary>
    public sealed class StorageRegistry
    {
        private readonly List<IMaterialSource> sources = new List<IMaterialSource>();

        public IReadOnlyList<IMaterialSource> Sources => sources;

        public void Register(IMaterialSource source)
        {
            if (source != null && !sources.Contains(source)) sources.Add(source);
        }

        public void Unregister(IMaterialSource source) => sources.Remove(source);
    }
}
