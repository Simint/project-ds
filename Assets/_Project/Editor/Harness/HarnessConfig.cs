using System;
using System.IO;
using UnityEngine;

namespace Game.Editor.Harness
{
    /// <summary>프로젝트 루트의 harness.config.json을 읽는다. Python 검증기와 같은 파일을 쓴다.</summary>
    [Serializable]
    public sealed class HarnessConfig
    {
        public string view = "SideScroller";
        public int pixelsPerUnit = 16;
        public float screenVerticalUnits = 11.25f;
        public string filterMode = "Bilinear";
        public string artRoot = "Assets/_Project/Art";
        public string[] bottomPivotFolders = Array.Empty<string>();
        public string[] sortingLayers = Array.Empty<string>();
        public string[] physicsLayers = Array.Empty<string>();
        public string[] scanRoots = { "Assets/_Project" };

        private static HarnessConfig cached;
        private static DateTime cachedWriteTime;

        public static string ConfigPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "harness.config.json"));

        public static HarnessConfig Load()
        {
            string path = ConfigPath;
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Harness] {path} 없음. 기본값을 사용합니다.");
                return cached ??= new HarnessConfig();
            }

            DateTime t = File.GetLastWriteTimeUtc(path);
            if (cached != null && t == cachedWriteTime) return cached;

            try
            {
                cached = JsonUtility.FromJson<HarnessConfig>(File.ReadAllText(path));
                cachedWriteTime = t;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Harness] harness.config.json 파싱 실패: {e.Message}");
                cached ??= new HarnessConfig();
            }
            return cached;
        }

        public FilterMode ResolvedFilterMode => filterMode switch
        {
            "Point" => UnityEngine.FilterMode.Point,
            "Trilinear" => UnityEngine.FilterMode.Trilinear,
            _ => UnityEngine.FilterMode.Bilinear,
        };

        public static string Normalize(string assetPath) => assetPath.Replace('\\', '/').TrimEnd('/');

        public bool IsUnderArtRoot(string assetPath) =>
            Normalize(assetPath).StartsWith(Normalize(artRoot) + "/", StringComparison.OrdinalIgnoreCase);

        public bool UsesBottomPivot(string assetPath)
        {
            string p = Normalize(assetPath);
            foreach (string folder in bottomPivotFolders)
                if (p.StartsWith(Normalize(folder) + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
