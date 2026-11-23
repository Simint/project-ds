using System.Collections.Generic;
using UnityEngine;

namespace Game.Crafting
{
    /// <summary>모든 RecipeDefinition의 목록. 작업대 UI가 이 목록을 읽어 제작 가능 레시피를 보여준다.</summary>
    [CreateAssetMenu(menuName = "Game/Crafting/Recipe Database")]
    public sealed class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private RecipeDefinition[] recipes = System.Array.Empty<RecipeDefinition>();

        private Dictionary<string, RecipeDefinition> lookup;

        public IReadOnlyList<RecipeDefinition> Recipes => recipes;

        public RecipeDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out RecipeDefinition def) ? def : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, RecipeDefinition>();
            foreach (RecipeDefinition recipe in recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.Id)) continue;
                if (!lookup.TryAdd(recipe.Id, recipe))
                    Debug.LogError($"[RecipeDatabase] 레시피 id '{recipe.Id}'가 중복됩니다.", this);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
