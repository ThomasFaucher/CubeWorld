using CubeWorld.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    public sealed class CraftingSystemTests
    {
        private CraftingSystem crafting;
        private ItemDefinition wood;
        private ItemDefinition plank;

        [SetUp]
        public void SetUp()
        {
            crafting = new CraftingSystem();
            wood = TestScriptableObjectFactory.CreateItem("Wood");
            plank = TestScriptableObjectFactory.CreateItem("Plank");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(wood);
            Object.DestroyImmediate(plank);
        }

        [Test]
        public void CanCraft_ReturnsTrue_WhenInventoryHasAllIngredients()
        {
            var inventory = new Inventory(4);
            inventory.TryAdd(wood, 3);
            CraftingRecipeDefinition recipe = TestScriptableObjectFactory.CreateRecipe(
                new[] { new CraftingRecipeDefinition.Ingredient { Item = wood, Quantity = 2 } },
                plank,
                1
            );

            Assert.IsTrue(crafting.CanCraft(inventory, recipe));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void CanCraft_ReturnsFalse_WhenIngredientQuantityMissing()
        {
            var inventory = new Inventory(4);
            inventory.TryAdd(wood, 1);
            CraftingRecipeDefinition recipe = TestScriptableObjectFactory.CreateRecipe(
                new[] { new CraftingRecipeDefinition.Ingredient { Item = wood, Quantity = 2 } },
                plank,
                1
            );

            Assert.IsFalse(crafting.CanCraft(inventory, recipe));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void TryCraft_ConsumesIngredients_AndAddsOutput()
        {
            var inventory = new Inventory(4);
            inventory.TryAdd(wood, 5);
            CraftingRecipeDefinition recipe = TestScriptableObjectFactory.CreateRecipe(
                new[] { new CraftingRecipeDefinition.Ingredient { Item = wood, Quantity = 2 } },
                plank,
                3
            );

            bool crafted = crafting.TryCraft(inventory, recipe);

            Assert.IsTrue(crafted);
            Assert.AreEqual(3, inventory.CountOf(wood), "2 des 5 bois doivent être consommés.");
            Assert.AreEqual(3, inventory.CountOf(plank));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void TryCraft_ReturnsFalse_AndLeavesInventoryUntouched_WhenMissingIngredients()
        {
            var inventory = new Inventory(4);
            inventory.TryAdd(wood, 1);
            CraftingRecipeDefinition recipe = TestScriptableObjectFactory.CreateRecipe(
                new[] { new CraftingRecipeDefinition.Ingredient { Item = wood, Quantity = 2 } },
                plank,
                1
            );

            bool crafted = crafting.TryCraft(inventory, recipe);

            Assert.IsFalse(crafted);
            Assert.AreEqual(1, inventory.CountOf(wood), "Un craft raté ne doit rien consommer.");
            Assert.AreEqual(0, inventory.CountOf(plank));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void TryCraft_ConsumesMultipleIngredients()
        {
            var iron = TestScriptableObjectFactory.CreateItem("Iron");
            var inventory = new Inventory(4);
            inventory.TryAdd(wood, 4);
            inventory.TryAdd(iron, 2);
            CraftingRecipeDefinition recipe = TestScriptableObjectFactory.CreateRecipe(
                new[]
                {
                    new CraftingRecipeDefinition.Ingredient { Item = wood, Quantity = 4 },
                    new CraftingRecipeDefinition.Ingredient { Item = iron, Quantity = 2 },
                },
                plank,
                1
            );

            Assert.IsTrue(crafting.TryCraft(inventory, recipe));
            Assert.AreEqual(0, inventory.CountOf(wood));
            Assert.AreEqual(0, inventory.CountOf(iron));
            Assert.AreEqual(1, inventory.CountOf(plank));

            Object.DestroyImmediate(iron);
            Object.DestroyImmediate(recipe);
        }
    }
}
