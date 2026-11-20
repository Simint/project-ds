using System.IO;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// 저장 파일 하나(슬롯)를 디스크에 읽고 쓴다 (ARCHITECTURE.md 4.10). 첫 버전은 자동 저장 1칸
    /// (docs/design/GAME_DESIGN.md 9). DTO를 만드는 건 호출하는 쪽(MapLoader)이 한다 — 이 클래스는
    /// 파일 입출력만 안다.
    /// </summary>
    public sealed class SaveService
    {
        private const string FileName = "slot0.json";

        private readonly string saveDirectory;

        public SaveService(string saveDirectoryOverride = null)
        {
            saveDirectory = saveDirectoryOverride ?? Path.Combine(Application.persistentDataPath, "saves");
        }

        public void Save(WorldSaveData data)
        {
            Directory.CreateDirectory(saveDirectory);
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(Path.Combine(saveDirectory, FileName), json);
        }

        /// <summary>저장 파일이 없으면(첫 실행) null을 돌려준다.</summary>
        public WorldSaveData Load()
        {
            string path = Path.Combine(saveDirectory, FileName);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<WorldSaveData>(json);
        }
    }
}
