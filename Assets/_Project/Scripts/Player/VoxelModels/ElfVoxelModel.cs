using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Elfe chibi : élégant, oreilles pointues, mèches et arc fin.</summary>
    internal static class ElfVoxelModel
    {
        private const int HeightVoxels = 28;

        private static readonly Color32 Skin = new(245, 215, 188, 255);
        private static readonly Color32 Hair = new(220, 195, 90, 255);
        private static readonly Color32 HairLight = new(240, 220, 130, 255);
        private static readonly Color32 Eye = new(50, 180, 120, 255);
        private static readonly Color32 Tunic = new(62, 110, 78, 255);
        private static readonly Color32 TunicLight = new(88, 145, 105, 255);
        private static readonly Color32 Pants = new(48, 72, 58, 255);
        private static readonly Color32 Trim = new(180, 150, 90, 255);
        private static readonly Color32 Boot = new(58, 48, 38, 255);
        private static readonly Color32 Wood = new(140, 210, 160, 255);
        private static readonly Color32 Leaf = new(90, 180, 110, 255);

        public static Mesh Build(float targetHeight)
        {
            float unit = targetHeight / HeightVoxels;
            var mesh = new PlayerVoxelMeshData();
            int seed = 0;

            Add(mesh, new Vector3Int(1, 0, -2), 3, 2, 5, PlayerVoxelShape.Box, Boot, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 0, -2), 3, 2, 5, PlayerVoxelShape.Box, Boot, unit, ref seed);
            Add(mesh, new Vector3Int(1, 0, 2), 2, 1, 1, PlayerVoxelShape.Box, Trim, unit, ref seed);
            Add(mesh, new Vector3Int(-3, 0, 2), 2, 1, 1, PlayerVoxelShape.Box, Trim, unit, ref seed);

            Add(mesh, new Vector3Int(1, 2, -2), 3, 5, 3, PlayerVoxelShape.Column, Pants, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 2, -2), 3, 5, 3, PlayerVoxelShape.Column, Pants, unit, ref seed);

            Add(mesh, new Vector3Int(-4, 7, -3), 8, 1, 5, PlayerVoxelShape.Box, Trim, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 8, -3), 8, 4, 5, PlayerVoxelShape.Box, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 9, -2), 4, 2, 1, PlayerVoxelShape.Box, TunicLight, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 10, -2), 2, 1, 1, PlayerVoxelShape.Box, Leaf, unit, ref seed);

            Add(mesh, new Vector3Int(4, 9, -2), 2, 3, 2, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 9, -2), 2, 3, 2, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(4, 7, -2), 2, 2, 2, PlayerVoxelShape.Sphere, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 7, -2), 2, 2, 2, PlayerVoxelShape.Sphere, Skin, unit, ref seed);

            Add(mesh, new Vector3Int(-1, 12, -2), 2, 1, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);

            const int headSize = 11;
            var headOrigin = new Vector3Int(-5, 13, -5);
            AddHead(mesh, headOrigin, headSize, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 13, -6), 12, 9, 12, PlayerVoxelShape.Dome, Hair, unit, ref seed);
            Add(mesh, new Vector3Int(4, 14, -3), 2, 6, 2, PlayerVoxelShape.Column, Hair, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 14, -3), 2, 6, 2, PlayerVoxelShape.Column, Hair, unit, ref seed);
            Add(mesh, new Vector3Int(4, 19, -3), 2, 2, 2, PlayerVoxelShape.Sphere, HairLight, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 19, -3), 2, 2, 2, PlayerVoxelShape.Sphere, HairLight, unit, ref seed);
            PlayerVoxelCuteFace.AddSparkles(mesh, headOrigin, headSize, unit, ref seed);

            // Oreilles pointues expressives.
            Add(mesh, new Vector3Int(5, 16, -3), 3, 2, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(7, 17, -3), 2, 2, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(8, 18, -3), 1, 1, 1, PlayerVoxelShape.Box, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(-8, 16, -3), 3, 2, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(-10, 17, -3), 2, 2, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(-11, 18, -3), 1, 1, 1, PlayerVoxelShape.Box, Skin, unit, ref seed);

            // Arc elfique + feuille décorative.
            Add(mesh, new Vector3Int(-1, 10, 2), 1, 5, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 12, 2), 2, 1, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(0, 12, 2), 2, 1, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 13, 2), 1, 1, 1, PlayerVoxelShape.Box, Leaf, unit, ref seed);

            return mesh.ToMesh("PlayerVoxelModel_Elf");
        }

        private static void AddHead(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int headSize,
            float unit,
            ref int seed
        )
        {
            Color32 HeadColorAt(int x, int y, int z) =>
                PlayerVoxelCuteFace.PaintSphere(x, y, z, headSize, Skin, Hair, Eye);

            Add(mesh, origin, headSize, headSize, headSize, PlayerVoxelShape.Sphere, HeadColorAt, unit, ref seed);
        }

        private static void Add(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            PlayerVoxelShape shape,
            Color32 color,
            float unit,
            ref int seed
        ) => PlayerVoxelMeshCore.AddPart(mesh, origin, sx, sy, sz, shape, color, unit, seed++);

        private static void Add(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            PlayerVoxelShape shape,
            Func<int, int, int, Color32> colorAt,
            float unit,
            ref int seed
        ) => PlayerVoxelMeshCore.AddPart(mesh, origin, sx, sy, sz, shape, colorAt, unit, seed++);
    }
}
