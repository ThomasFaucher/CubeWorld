using CubeWorld.CharacterModel.Core;
using NUnit.Framework;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    public sealed class VoxelGridTests
    {
        [Test]
        public void NewGrid_IsEmptyEverywhere()
        {
            var grid = new VoxelGrid(3, 3, 3);

            Assert.IsFalse(grid.IsSolid(1, 1, 1));
        }

        [Test]
        public void Set_ThenGet_RoundTripsColor_WithAlphaForcedOpaque()
        {
            var grid = new VoxelGrid(3, 3, 3);
            var translucentRed = new Color32(255, 0, 0, 10);

            grid.Set(1, 1, 1, translucentRed);

            Color32 stored = grid.Get(1, 1, 1);
            Assert.AreEqual((byte)255, stored.a, "Set() doit forcer l'alpha à opaque.");
            Assert.AreEqual((byte)255, stored.r);
            Assert.IsTrue(grid.IsSolid(1, 1, 1));
        }

        [Test]
        public void Set_OutOfBounds_IsIgnored_NoException()
        {
            var grid = new VoxelGrid(2, 2, 2);

            Assert.DoesNotThrow(() => grid.Set(5, 5, 5, Color.white));
            Assert.IsFalse(grid.IsSolid(5, 5, 5));
        }

        [Test]
        public void Clear_MakesVoxelEmptyAgain()
        {
            var grid = new VoxelGrid(3, 3, 3);
            grid.Set(1, 1, 1, Color.white);

            grid.Clear(1, 1, 1);

            Assert.IsFalse(grid.IsSolid(1, 1, 1));
        }

        [Test]
        public void InBounds_RejectsNegativeAndOverflowingIndices()
        {
            var grid = new VoxelGrid(2, 3, 4);

            Assert.IsTrue(grid.InBounds(0, 0, 0));
            Assert.IsTrue(grid.InBounds(1, 2, 3));
            Assert.IsFalse(grid.InBounds(-1, 0, 0));
            Assert.IsFalse(grid.InBounds(2, 0, 0));
            Assert.IsFalse(grid.InBounds(0, 3, 0));
            Assert.IsFalse(grid.InBounds(0, 0, 4));
        }

        [Test]
        public void PaintFrontmost_RecolorsTheMostAdvancedSolidVoxel_InTheColumn()
        {
            var grid = new VoxelGrid(1, 1, 3);
            grid.Set(0, 0, 0, Color.white);
            grid.Set(0, 0, 1, Color.white);
            // z = 2 (le plus avancé) laissé vide : PaintFrontmost doit retomber sur z = 1.

            grid.PaintFrontmost(0, 0, Color.red);

            Assert.AreEqual((Color32)Color.red, grid.Get(0, 0, 1));
            Assert.AreEqual((Color32)Color.white, grid.Get(0, 0, 0));
            Assert.IsFalse(grid.IsSolid(0, 0, 2));
        }

        [Test]
        public void PaintFrontmost_OnEmptyColumn_DoesNothing()
        {
            var grid = new VoxelGrid(1, 1, 3);

            Assert.DoesNotThrow(() => grid.PaintFrontmost(0, 0, Color.red));
            Assert.IsFalse(grid.IsSolid(0, 0, 0));
            Assert.IsFalse(grid.IsSolid(0, 0, 1));
            Assert.IsFalse(grid.IsSolid(0, 0, 2));
        }
    }
}
