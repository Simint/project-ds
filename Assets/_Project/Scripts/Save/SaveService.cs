using System;
using System.IO;
using Game.Core;
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

        /// <summary>임시 파일에 먼저 쓰고 나서 실제 파일로 바꿔 끼운다 — 쓰다가 중간에 멈추면(강종,
        /// 디스크 꽉 참 등) 기존 저장 파일은 그대로 남는다. 절반만 쓰인 파일이 생기지 않는다.
        /// 디스크 오류(권한, 공간 부족, 다른 프로그램이 파일을 잡고 있음)는 예외로 던지지 않고 오류 로그를 남긴 뒤
        /// false를 돌려준다 — 저장이 실패해도 맵 이동·종료 같은 호출한 쪽의 흐름은 끊기지 않아야 한다.</summary>
        public bool Save(WorldSaveData data)
        {
            string path = Path.Combine(saveDirectory, FileName);
            string tempPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(saveDirectory);
                string json = JsonUtility.ToJson(data);
                File.WriteAllText(tempPath, json);

                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[Save] 저장하지 못했습니다(기존 저장 파일은 그대로 둡니다): {e.Message}");
                TryDeleteTemp(tempPath);
                return false;
            }
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // 남은 임시 파일은 다음 저장이 덮어쓴다.
            }
        }

        /// <summary>새 게임을 시작할 때 기존 저장을 지운다(자동 저장 1칸, 타이틀에서 확인을 받은 뒤).</summary>
        public void Delete()
        {
            string path = Path.Combine(saveDirectory, FileName);
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (System.Exception e) when (e is IOException || e is System.UnauthorizedAccessException)
            {
                // 지우지 못해도 새 게임은 그대로 시작한다 — 첫 자동 저장이 이 파일을 덮어쓴다.
                Game.Core.GameLog.Warning("Save", $"기존 저장을 지우지 못했습니다(새 게임은 계속): {e.Message}");
            }
        }

        /// <summary>저장 파일이 없으면(첫 실행) null을 돌려준다. 옛 버전 파일도 그대로 읽는다 — 새로
        /// 생긴 필드는 JsonUtility가 C# 기본값으로 채운다 (WorldSaveData.CurrentVersion 참고). 파일이
        /// 손상돼 읽을 수 없으면(깨진 JSON 등) 경고만 남기고 null을 돌려준다 — 디스크 입출력은 에이전트가
        /// 통제할 수 없는 경계라 여기서 검증한다.</summary>
        public WorldSaveData Load()
        {
            string path = Path.Combine(saveDirectory, FileName);
            if (!File.Exists(path)) return null;

            WorldSaveData data;
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<WorldSaveData>(json);
            }
            catch (Exception e)
            {
                GameLog.Warning("Save", $"저장 파일이 손상되어 무시합니다: {e.Message}");
                return null;
            }

            if (data != null && data.version < WorldSaveData.CurrentVersion)
                GameLog.Info("Save", $"옛 버전 저장 파일(v{data.version})을 불러와 기본값으로 채웠습니다 (현재 v{WorldSaveData.CurrentVersion}).");
            return data;
        }
    }
}
