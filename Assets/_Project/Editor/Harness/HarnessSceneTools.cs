using System.Collections.Generic;
using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Harness
{
    /// <summary>
    /// 열린 씬을 하네스 규칙으로 검사하고, 문제가 있는 오브젝트를 Console에서 클릭해 찾을 수 있게 로그를 남긴다.
    /// Python 검증기(tools/harness/validate.py)와 같은 규칙 번호를 쓴다.
    /// </summary>
    public static class HarnessSceneTools
    {
        [MenuItem("Tools/Harness/Validate Open Scenes", priority = 20)]
        public static void ValidateOpenScenes()
        {
            int issues = 0;

            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r is not SpriteRenderer && r is not UnityEngine.Tilemaps.TilemapRenderer) continue;
                if (r.sortingLayerID == 0)
                {
                    Debug.LogWarning($"[Harness HX203] '{Path(r.transform)}' 가 Default Sorting Layer에 있습니다.", r);
                    issues++;
                }
                else if (!SortingLayer.IsValid(r.sortingLayerID))
                {
                    Debug.LogError($"[Harness HX204] '{Path(r.transform)}' 의 Sorting Layer가 삭제된 레이어입니다.", r);
                    issues++;
                }

                // Props 레이어 오브젝트는 플레이어가 통과해야 한다: 루트에 비트리거 콜라이더가 있으면 경고
                if (r.sortingLayerName == GameLayers.Sorting.Props)
                {
                    Transform root = r.transform.parent != null ? r.transform.parent : r.transform;
                    foreach (Collider2D c in root.GetComponents<Collider2D>())
                    {
                        if (c.isTrigger || c.gameObject.layer == GameLayers.Ground || c.gameObject.layer == GameLayers.Structure) continue;
                        Debug.LogWarning($"[Harness HX206] Props 오브젝트 '{Path(root)}' 에 비트리거 콜라이더가 있습니다. 통과시키려면 트리거로, 막으려면 Body 자식(Layer Ground)으로 옮기세요.", c);
                        issues++;
                    }
                }
            }

            foreach (Rigidbody2D rb in Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include))
            {
                if (rb.gameObject.layer != GameLayers.Player) continue;
                if (rb.interpolation == RigidbodyInterpolation2D.None)
                {
                    Debug.LogWarning($"[Harness HX207] 플레이어 Rigidbody2D '{Path(rb.transform)}' Interpolate가 꺼져 있습니다 (화면 떨림).", rb);
                    issues++;
                }
                if (rb.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
                {
                    Debug.LogWarning($"[Harness HX207] 플레이어 Rigidbody2D '{Path(rb.transform)}' Collision Detection이 Continuous가 아닙니다.", rb);
                    issues++;
                }
            }

            issues += CheckPersistentIds(fix: false);

            if (issues == 0) Debug.Log("[Harness] 열린 씬 검사 통과");
            else Debug.Log($"[Harness] 열린 씬 검사: 문제 {issues}개 (로그를 클릭하면 오브젝트로 이동)");
        }

        [MenuItem("Tools/Harness/Fix Duplicate Persistent IDs", priority = 21)]
        public static void FixDuplicateIds()
        {
            int fixedCount = CheckPersistentIds(fix: true);
            if (fixedCount > 0) EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[Harness] PersistentId {fixedCount}개 재생성. 씬을 저장하세요.");
        }

        private static int CheckPersistentIds(bool fix)
        {
            var seen = new Dictionary<string, PersistentId>();
            int count = 0;
            foreach (PersistentId pid in Object.FindObjectsByType<PersistentId>(FindObjectsInactive.Include))
            {
                if (string.IsNullOrEmpty(pid.Id))
                {
                    if (fix) { pid.Regenerate(); count++; }
                    else { Debug.LogWarning($"[Harness HX302] '{Path(pid.transform)}' PersistentId가 비어 있습니다.", pid); count++; }
                    continue;
                }
                if (seen.TryGetValue(pid.Id, out PersistentId first))
                {
                    if (fix) { pid.Regenerate(); count++; }
                    else
                    {
                        Debug.LogError($"[Harness HX301] '{Path(pid.transform)}' 와 '{Path(first.transform)}' 의 PersistentId가 같습니다. Tools > Harness > Fix Duplicate Persistent IDs", pid);
                        count++;
                    }
                    continue;
                }
                seen.Add(pid.Id, pid);
            }
            return count;
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }
    }
}
