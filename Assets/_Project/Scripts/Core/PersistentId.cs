using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Core
{
    /// <summary>
    /// 고정 맵에 손으로 배치한, 상태를 저장해야 하는 오브젝트(채집물, 상자 등)의 고유 ID.
    /// - 프리팹 에셋에서는 비워 둔다.
    /// - 씬에 놓으면 에디터가 자동으로 생성한다.
    /// - 복제(Ctrl+D)하면 ID가 겹칠 수 있다: Tools > Harness > Fix Duplicate Persistent IDs,
    ///   또는 컴포넌트 우클릭 > Regenerate. 하네스 검증기(HX301)가 중복을 찾는다.
    /// 플레이 중 생성되는 오브젝트(설치한 건설물 등)에는 쓰지 않는다. 그건 셀 좌표 + 정의 id로 저장한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PersistentId : MonoBehaviour
    {
        [SerializeField] private string persistentId = "";

        public string Id => persistentId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(persistentId)) return;
            if (PrefabUtility.IsPartOfPrefabAsset(this)) return;
            if (Application.isPlaying) return;
            // OnValidate 안에서 직접 직렬화 값을 바꾸면 경고가 나는 경우가 있어 한 틱 미룬다.
            EditorApplication.delayCall += AssignIfStillEmpty;
        }

        private void AssignIfStillEmpty()
        {
            if (this == null || !string.IsNullOrEmpty(persistentId)) return;
            if (PrefabUtility.IsPartOfPrefabAsset(this)) return;
            Regenerate();
        }

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            Undo.RecordObject(this, "Regenerate Persistent Id");
            persistentId = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
