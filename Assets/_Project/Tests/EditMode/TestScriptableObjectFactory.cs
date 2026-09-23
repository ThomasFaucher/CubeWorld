using System;
using System.Reflection;
using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    /// <summary>
    /// Construit des ScriptableObject de test (ItemDefinition, CraftingRecipeDefinition) sans
    /// passer par l'inspecteur : ces définitions n'exposent que des setters privés (données
    /// d'authoring), donc les tests écrivent directement les champs sérialisés par réflexion.
    /// </summary>
    internal static class TestScriptableObjectFactory
    {
        public static ItemDefinition CreateItem(string id = "TestItem", int maxStackSize = 99)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(item, "_id", id);
            SetField(item, "_displayName", id);
            SetField(item, "_maxStackSize", maxStackSize);
            return item;
        }

        public static CraftingRecipeDefinition CreateRecipe(
            CraftingRecipeDefinition.Ingredient[] ingredients,
            ItemDefinition outputItem,
            int outputQuantity
        )
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            SetField(recipe, "_ingredients", ingredients);
            SetField(recipe, "_outputItem", outputItem);
            SetField(recipe, "_outputQuantity", outputQuantity);
            return recipe;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target
                .GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(
                    $"{target.GetType().Name}.{fieldName} introuvable (a-t-il été renommé ?)."
                );
            }

            field.SetValue(target, value);
        }
    }
}
