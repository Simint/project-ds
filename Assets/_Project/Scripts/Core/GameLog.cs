using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Game.Core
{
    /// <summary>
    /// Debug.Log 대신 쓴다 (.claude/rules/csharp.md). Info는 에디터·개발 빌드에만 남고
    /// 배포 빌드에서는 호출 자체가 빠진다 ([Conditional]). 경고·오류는 그대로 둔다.
    /// </summary>
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string tag, string message)
        {
            Debug.Log($"[{tag}] {message}");
        }

        public static void Warning(string tag, string message, Object context = null)
        {
            Debug.LogWarning($"[{tag}] {message}", context);
        }

        public static void Error(string tag, string message, Object context = null)
        {
            Debug.LogError($"[{tag}] {message}", context);
        }
    }
}
