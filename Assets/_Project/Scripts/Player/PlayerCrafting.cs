using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>Wrapper fin autour de Combat.CraftingSystem, lié à l'Inventory du joueur.</summary>
    public sealed class PlayerCrafting : MonoBehaviour
    {
        private PlayerInventory inventory;
        private readonly CraftingSystem craftingSystem = new();

        public void Bind(PlayerInventory playerInventory)
        {
            inventory = playerInventory;
        }

        public bool CanCraft(CraftingRecipeDefinition recipe) =>
            craftingSystem.CanCraft(inventory.Contents, recipe);

        public bool TryCraft(CraftingRecipeDefinition recipe) =>
            craftingSystem.TryCraft(inventory.Contents, recipe);
    }
}
