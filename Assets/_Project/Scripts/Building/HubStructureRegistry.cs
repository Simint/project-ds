using System.Collections.Generic;
using Game.Save;

namespace Game.Building
{
    /// <summary>
    /// 지금 설치된 은신처 구조물 전체의 기억 장소 (ARCHITECTURE.md 3.4의 "맵 상태"와 같은 역할을
    /// 구조물에 대해 한다). 은신처 씬은 다른 맵처럼 오갈 때마다 통째로 언로드되므로, BuildGrid가 씬을
    /// 나가기 직전에 여기로 현재 상태를 밀어 넣고, 다시 들어올 때 여기서 읽어 복원한다. 저장 파일을
    /// 불러올 때는 GameBootstrap이 SetCurrent로 한 번 채운다.
    /// </summary>
    public sealed class HubStructureRegistry
    {
        private List<StructureSaveData> current = new List<StructureSaveData>();

        public IReadOnlyList<StructureSaveData> Current => current;

        public void SetCurrent(List<StructureSaveData> value) => current = value ?? new List<StructureSaveData>();
    }
}
