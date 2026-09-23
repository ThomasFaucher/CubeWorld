using CubeWorld.CharacterModel.Core;
using NUnit.Framework;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    public sealed class VoxelStamperTests
    {
        [Test]
        public void Stamp_Box_FillsTheEntireRegion()
        {
            var grid = new VoxelGrid(4, 4, 4);

            VoxelStamper.Stamp(grid, Vector3Int.zero, 4, 4, 4, VoxelShape.Box, Color.white);

            for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
            for (int z = 0; z < 4; z++)
            {
                Assert.IsTrue(grid.IsSolid(x, y, z), $"({x},{y},{z}) devrait être plein (Box).");
            }
        }

        [Test]
        public void Stamp_Sphere_FillsCenter_ButNotCorners()
        {
            var grid = new VoxelGrid(5, 5, 5);

            VoxelStamper.Stamp(grid, Vector3Int.zero, 5, 5, 5, VoxelShape.Sphere, Color.white);

            Assert.IsTrue(grid.IsSolid(2, 2, 2), "Le centre d'une sphère inscrite doit être plein.");
            Assert.IsFalse(grid.IsSolid(0, 0, 0), "Un coin doit rester hors de la sphère inscrite.");
        }

        [Test]
        public void Stamp_Column_IgnoresYAxis_ForItsCrossSection()
        {
            var grid = new VoxelGrid(5, 3, 5);

            VoxelStamper.Stamp(grid, Vector3Int.zero, 5, 3, 5, VoxelShape.Column, Color.white);

            // Colonne : la section (X/Z) ne dépend pas de Y, donc le centre est plein à
            // toutes les hauteurs, et le coin XZ est vide à toutes les hauteurs.
            for (int y = 0; y < 3; y++)
            {
                Assert.IsTrue(grid.IsSolid(2, y, 2), $"Centre de colonne, y={y}");
                Assert.IsFalse(grid.IsSolid(0, y, 0), $"Coin hors colonne, y={y}");
            }
        }

        [Test]
        public void Stamp_RespectsOriginOffset()
        {
            var grid = new VoxelGrid(6, 6, 6);
            var origin = new Vector3Int(2, 1, 0);

            VoxelStamper.Stamp(grid, origin, 2, 2, 2, VoxelShape.Box, Color.white);

            Assert.IsTrue(grid.IsSolid(2, 1, 0));
            Assert.IsTrue(grid.IsSolid(3, 2, 1));
            Assert.IsFalse(grid.IsSolid(0, 0, 0), "Hors du tampon décalé, ça doit rester vide.");
        }

        [Test]
        public void Stamp_WithColorFunction_ReceivesLocalBufferCoordinates()
        {
            var grid = new VoxelGrid(3, 1, 1);
            var seenCoords = new bool[3];

            VoxelStamper.Stamp(
                grid,
                Vector3Int.zero,
                3,
                1,
                1,
                VoxelShape.Box,
                (x, y, z) =>
                {
                    seenCoords[x] = true;
                    Assert.AreEqual(0, y);
                    Assert.AreEqual(0, z);
                    return x == 0 ? (Color32)Color.red : (Color32)Color.blue;
                }
            );

            Assert.IsTrue(seenCoords[0] && seenCoords[1] && seenCoords[2]);
            Assert.AreEqual((Color32)Color.red, grid.Get(0, 0, 0));
            Assert.AreEqual((Color32)Color.blue, grid.Get(1, 0, 0));
            Assert.AreEqual((Color32)Color.blue, grid.Get(2, 0, 0));
        }
    }
}
