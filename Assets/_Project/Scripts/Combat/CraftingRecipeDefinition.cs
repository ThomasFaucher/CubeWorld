using System;
using UnityEngine;

namespace CubeWorld.Combat
{
    [CreateAssetMenu(
        fileName = "CraftingRecipeDefinition",
        menuName = "CubeWorld/Crafting Recipe Definition"
    )]
    public sealed class CraftingRecipeDefinition : ScriptableObject
    {
        [SerializeField]
        private Ingredient[] _ingredients = Array.Empty<Ingredient>();

        [SerializeField]
        private ItemDefinition _outputItem;

        [SerializeField]
        private int _outputQuantity = 1;

        public Ingredient[] Ingredients => _ingredients;
        public ItemDefinition OutputItem => _outputItem;
        public int OutputQuantity => _outputQuantity;

        [Serializable]
        public struct Ingredient
        {
            public ItemDefinition Item;
            public int Quantity;
        }
    }
}
