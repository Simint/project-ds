namespace Game.Crafting
{
    /// <summary>
    /// 제작·건설·수리가 재료를 찾는 단 하나의 창구 (docs/design/GAME_DESIGN.md 8, ARCHITECTURE.md 4.4).
    /// 은신처의 모든 보관함(StorageRegistry) + 가방을 하나로 묶는다.
    ///
    /// 뺄 때 순서: <b>보관함을 먼저 쓰고, 모자란 만큼만 가방에서 뺀다.</b> 가방은 들고 다니며 계속
    /// 채집하는 공간이라 최대한 비워 두는 쪽이 유리하고, 보관함은 이미 쌓아 둔 창고이기 때문이다
    /// (이 순서가 유일한 규칙이다 — 바꾸려면 Remove 하나만 고치면 된다).
    /// </summary>
    public sealed class MaterialSourceAggregator : IMaterialSource
    {
        private readonly IMaterialSource bag;
        private readonly StorageRegistry storages;

        public MaterialSourceAggregator(IMaterialSource bag, StorageRegistry storages)
        {
            this.bag = bag;
            this.storages = storages;
        }

        public int CountOf(string itemId)
        {
            int total = bag != null ? bag.CountOf(itemId) : 0;
            if (storages != null)
                foreach (IMaterialSource source in storages.Sources)
                    total += source.CountOf(itemId);
            return total;
        }

        public int Remove(string itemId, int count)
        {
            int remaining = count;
            if (storages != null)
            {
                foreach (IMaterialSource source in storages.Sources)
                {
                    if (remaining <= 0) break;
                    remaining -= source.Remove(itemId, remaining);
                }
            }
            if (remaining > 0 && bag != null) remaining -= bag.Remove(itemId, remaining);
            return count - remaining;
        }

        public bool HasAllIngredients(RecipeIngredient[] ingredients)
        {
            foreach (RecipeIngredient ingredient in ingredients)
                if (CountOf(ingredient.ItemId) < ingredient.Count) return false;
            return true;
        }

        /// <summary>전부 있는지 먼저 확인하고 나서 뺀다 — 중간에 모자라서 일부만 빠지는 일이 없다.</summary>
        public bool TryConsume(RecipeIngredient[] ingredients)
        {
            if (!HasAllIngredients(ingredients)) return false;
            foreach (RecipeIngredient ingredient in ingredients) Remove(ingredient.ItemId, ingredient.Count);
            return true;
        }
    }
}
