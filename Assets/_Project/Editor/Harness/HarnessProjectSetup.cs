using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Harness
{
    /// <summary>
    /// Tools > Harness > Apply Project Settings
    /// harness.config.json 기준으로 Sorting Layer, 물리 Layer, Layer Collision Matrix, Transparency Sort Mode를 맞춘다.
    /// 기존에 있던 다른 레이어는 지우지 않는다.
    /// </summary>
    public static class HarnessProjectSetup
    {
        // docs/RENDERING_RULES.md 3장. 하네스 물리 레이어끼리는 여기 있는 쌍만 충돌하고 나머지는 끈다.
        private static readonly string[,] CollidingPairs =
        {
            { "Player", "Ground" }, { "Player", "OneWayPlatform" }, { "Player", "Structure" },
            { "Player", "Enemy" }, { "Player", "Interactable" }, { "Player", "Pickup" }, { "Player", "EnemyAttack" },
            { "Enemy", "Ground" }, { "Enemy", "OneWayPlatform" }, { "Enemy", "Structure" }, { "Enemy", "PlayerAttack" },
            { "Pickup", "Ground" }, { "Pickup", "OneWayPlatform" }, { "Pickup", "Structure" },
        };

        private const int FirstUserLayer = 8;
        private const int LayerCount = 32;

        [MenuItem("Tools/Harness/Apply Project Settings", priority = 0)]
        public static void Apply()
        {
            HarnessConfig cfg = HarnessConfig.Load();
            bool ok = EditorUtility.DisplayDialog(
                "Harness",
                "harness.config.json 기준으로 다음을 맞춥니다.\n\n" +
                "• Sorting Layer 추가 및 순서 정렬\n" +
                "• 물리 Layer 추가 (빈 칸에만)\n" +
                "• Layer Collision Matrix (하네스 레이어끼리만)\n" +
                "• Transparency Sort Mode = Default (횡스크롤)\n\n" +
                "기존 레이어는 지우지 않습니다. 계속할까요?",
                "적용", "취소");
            if (!ok) return;

            var log = new StringBuilder();
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            ApplySortingLayers(tagManager, cfg.sortingLayers, log);
            ApplyPhysicsLayers(tagManager, cfg.physicsLayers, log);
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            ApplyCollisionMatrix(cfg.physicsLayers, log);
            ApplySortMode(log);

            AssetDatabase.SaveAssets();
            Debug.Log("[Harness] 프로젝트 설정 적용 완료\n" + (log.Length > 0 ? log.ToString() : "변경 없음"));
        }

        // ------------------------------------------------------------------ Sorting Layers
        private struct SortingEntry
        {
            public string Name;
            public long Id;
            public int Locked;
        }

        private static void ApplySortingLayers(SerializedObject tm, string[] desired, StringBuilder log)
        {
            SerializedProperty arr = tm.FindProperty("m_SortingLayers");
            var existing = new List<SortingEntry>();
            var used = new HashSet<long>();
            for (int i = 0; i < arr.arraySize; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                var entry = new SortingEntry
                {
                    Name = e.FindPropertyRelative("name").stringValue,
                    Id = e.FindPropertyRelative("uniqueID").longValue,
                    Locked = ReadInt(e.FindPropertyRelative("locked")),
                };
                existing.Add(entry);
                used.Add(entry.Id);
            }

            var desiredSet = new HashSet<string>(desired);
            var result = new List<SortingEntry>();

            // Default가 config에 없으면 맨 앞에 유지
            if (!desiredSet.Contains("Default"))
                foreach (SortingEntry e in existing)
                    if (e.Name == "Default") result.Add(e);

            foreach (string name in desired)
            {
                int idx = existing.FindIndex(e => e.Name == name);
                if (idx >= 0)
                {
                    result.Add(existing[idx]);
                }
                else
                {
                    result.Add(new SortingEntry { Name = name, Id = NewUniqueId(used), Locked = 0 });
                    log.AppendLine($"+ Sorting Layer '{name}'");
                }
            }

            foreach (SortingEntry e in existing)
            {
                if (desiredSet.Contains(e.Name) || e.Name == "Default") continue;
                result.Add(e);
                log.AppendLine($"! config에 없는 Sorting Layer '{e.Name}'는 맨 앞(가장 위에 그려짐)으로 옮겨 유지했습니다. 필요 없으면 지우세요.");
            }

            bool orderChanged = result.Count != existing.Count;
            for (int i = 0; !orderChanged && i < result.Count; i++)
                orderChanged = result[i].Name != existing[i].Name;
            if (!orderChanged) return;

            arr.arraySize = result.Count;
            for (int i = 0; i < result.Count; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = result[i].Name;
                e.FindPropertyRelative("uniqueID").longValue = result[i].Id;
                WriteInt(e.FindPropertyRelative("locked"), result[i].Locked);
            }
            log.AppendLine("Sorting Layer 순서: " + string.Join(" > ", result.ConvertAll(r => r.Name)));
        }

        private static long NewUniqueId(HashSet<long> used)
        {
            var rng = new System.Random();
            long id;
            do { id = rng.Next(1, int.MaxValue); } while (used.Contains(id));
            used.Add(id);
            return id;
        }

        private static int ReadInt(SerializedProperty p) =>
            p == null ? 0 : p.propertyType == SerializedPropertyType.Boolean ? (p.boolValue ? 1 : 0) : p.intValue;

        private static void WriteInt(SerializedProperty p, int v)
        {
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v != 0;
            else p.intValue = v;
        }

        // ------------------------------------------------------------------ Physics Layers
        private static void ApplyPhysicsLayers(SerializedObject tm, string[] desired, StringBuilder log)
        {
            SerializedProperty layers = tm.FindProperty("layers");
            foreach (string name in desired)
            {
                bool exists = false;
                for (int i = 0; i < LayerCount && !exists; i++)
                    exists = layers.GetArrayElementAtIndex(i).stringValue == name;
                if (exists) continue;

                int slot = -1;
                for (int i = FirstUserLayer; i < LayerCount; i++)
                {
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { slot = i; break; }
                }
                if (slot < 0)
                {
                    log.AppendLine($"! 물리 Layer '{name}'를 넣을 빈 칸이 없습니다 (8~31 모두 사용 중).");
                    continue;
                }
                layers.GetArrayElementAtIndex(slot).stringValue = name;
                log.AppendLine($"+ 물리 Layer '{name}' (index {slot})");
            }
        }

        // ------------------------------------------------------------------ Collision Matrix
        private static void ApplyCollisionMatrix(string[] harnessLayers, StringBuilder log)
        {
            var allowed = new HashSet<(int, int)>();
            for (int i = 0; i < CollidingPairs.GetLength(0); i++)
            {
                int a = LayerMask.NameToLayer(CollidingPairs[i, 0]);
                int b = LayerMask.NameToLayer(CollidingPairs[i, 1]);
                if (a < 0 || b < 0) continue;
                allowed.Add((Mathf.Min(a, b), Mathf.Max(a, b)));
            }

            var indices = new List<int>();
            foreach (string name in harnessLayers)
            {
                int idx = LayerMask.NameToLayer(name);
                if (idx >= 0) indices.Add(idx);
            }

            int changed = 0;
            foreach (int a in indices)
            {
                foreach (int b in indices)
                {
                    if (b < a) continue;
                    bool shouldCollide = allowed.Contains((a, b));
                    bool collides = !Physics2D.GetIgnoreLayerCollision(a, b);
                    if (collides == shouldCollide) continue;
                    Physics2D.IgnoreLayerCollision(a, b, !shouldCollide);
                    changed++;
                }
            }
            if (changed > 0) log.AppendLine($"Layer Collision Matrix: {changed}개 조합 변경");
        }

        // ------------------------------------------------------------------ Sort Mode
        private static void ApplySortMode(StringBuilder log)
        {
            if (GraphicsSettings.transparencySortMode != TransparencySortMode.Default)
            {
                GraphicsSettings.transparencySortMode = TransparencySortMode.Default;
                log.AppendLine("GraphicsSettings Transparency Sort Mode → Default");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) continue;
                var so = new SerializedObject(asset);
                SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
                if (mode == null || mode.intValue == (int)TransparencySortMode.Default) continue;
                mode.intValue = (int)TransparencySortMode.Default;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                log.AppendLine($"{path} Transparency Sort Mode → Default");
            }
        }
    }
}
