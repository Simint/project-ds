using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 모든 ItemDefinition을 id로 찾는 색인. 저장은 id 문자열로 하므로(ARCHITECTURE.md 2.2),
    /// 인벤토리·채집물·컨테이너가 id로부터 실제 정의를 찾을 때 이걸 거친다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = System.Array.Empty<ItemDefinition>();

        private Dictionary<string, ItemDefinition> lookup;

        public ItemDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out ItemDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, ItemDefinition>();
            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                if (!lookup.TryAdd(item.Id, item))
                    Debug.LogError($"[ItemDatabase] 아이템 id '{item.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
