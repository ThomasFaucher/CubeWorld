namespace CubeWorld.Combat
{
    /// <summary>Résolution de crafting, pure (comme MeleeAttackResolver) : vérifie/consomme les ingrédients d'une recette contre un Inventory.</summary>
    public sealed class CraftingSystem
    {
        public bool CanCraft(Inventory inventory, CraftingRecipeDefinition recipe)
        {
            foreach (CraftingRecipeDefinition.Ingredient ingredient in recipe.Ingredients)
            {
                if (!inventory.Contains(ingredient.Item, ingredient.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryCraft(Inventory inventory, CraftingRecipeDefinition recipe)
        {
            if (!CanCraft(inventory, recipe))
            {
                return false;
            }

            foreach (CraftingRecipeDefinition.Ingredient ingredient in recipe.Ingredients)
            {
                inventory.TryRemove(ingredient.Item, ingredient.Quantity);
            }

            return inventory.TryAdd(recipe.OutputItem, recipe.OutputQuantity);
        }
    }
}
