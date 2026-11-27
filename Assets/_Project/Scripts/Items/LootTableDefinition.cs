using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 상자·묻힌 상자 하나가 주는 아이템 구성 (docs/design/GAME_DESIGN.md 4.3의 7단계 범위). 보상 구성은 밸런스라
    /// 공개 씬·프리팹에 직접 적지 않고, 이 정의 에셋(비공개 Data/Loot/)을 참조만 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/Loot Table Definition")]
    public sealed class LootTableDefinition : ScriptableObject, IDefinition
    {
        [SerializeField] private string id = "";
        [SerializeField] private ItemAmount[] items = System.Array.Empty<ItemAmount>();

        public string Id => id;
        public IReadOnlyList<ItemAmount> Items => items;
    }
}
