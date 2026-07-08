using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Épéiste chibi : trapu, grosse tête, petites jambes, armure détaillée.</summary>
    internal static class SwordsmanVoxelModel
    {
        private const int HeightVoxels = 28;

        private static readonly Color32 Skin = new(238, 198, 169, 255);
        private static readonly Color32 Hair = new(35, 85, 210, 255);
        private static readonly Color32 Eye = new(70, 190, 200, 255);
        private static readonly Color32 Armor = new(100, 105, 120, 255);
        private static readonly Color32 ArmorLight = new(140, 145, 158, 255);
        private static readonly Color32 Tunic = new(72, 58, 110, 255);
        private static readonly Color32 Pants = new(58, 54, 62, 255);
        private static readonly Color32 Belt = new(214, 193, 150, 255);
        private static readonly Color32 Boot = new(40, 32, 30, 255);
        private static readonly Color32 Blade = new(200, 205, 220, 255);
        private static readonly Color32 Hilt = new(90, 60, 40, 255);

        public static Mesh Build(float targetHeight)
        {
            float unit = targetHeight / HeightVoxels;
            var mesh = new PlayerVoxelMeshData();
            int seed = 0;

            // Petites bottes arrondies.
            Add(mesh, new Vector3Int(0, 0, -2), 5, 2, 6, PlayerVoxelShape.Box, Boot, unit, ref seed);
            Add(mesh, new Vector3Int(-5, 0, -2), 5, 2, 6, PlayerVoxelShape.Box, Boot, unit, ref seed);
            Add(mesh, new Vector3Int(1, 0, 3), 3, 1, 1, PlayerVoxelShape.Box, ArmorLight, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 0, 3), 3, 1, 1, PlayerVoxelShape.Box, ArmorLight, unit, ref seed);

            // Jambes courtes et potelées.
            Add(mesh, new Vector3Int(0, 2, -2), 4, 4, 4, PlayerVoxelShape.Column, Pants, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 2, -2), 4, 4, 4, PlayerVoxelShape.Column, Pants, unit, ref seed);

            Add(mesh, new Vector3Int(-5, 6, -3), 10, 1, 6, PlayerVoxelShape.Box, Belt, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 6, -2), 2, 1, 1, PlayerVoxelShape.Box, ArmorLight, unit, ref seed);

            // Torse compact + plaque.
            Add(mesh, new Vector3Int(-5, 7, -3), 10, 5, 6, PlayerVoxelShape.Box, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-3, 8, -2), 6, 3, 1, PlayerVoxelShape.Box, Armor, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 9, -2), 2, 1, 1, PlayerVoxelShape.Box, ArmorLight, unit, ref seed);

            // Épaulettes.
            Add(mesh, new Vector3Int(5, 10, -2), 2, 2, 3, PlayerVoxelShape.Box, Armor, unit, ref seed);
            Add(mesh, new Vector3Int(-7, 10, -2), 2, 2, 3, PlayerVoxelShape.Box, Armor, unit, ref seed);

            // Bras courts + mains rondes.
            Add(mesh, new Vector3Int(5, 8, -2), 3, 3, 3, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-8, 8, -2), 3, 3, 3, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(5, 6, -2), 3, 2, 3, PlayerVoxelShape.Sphere, Armor, unit, ref seed);
            Add(mesh, new Vector3Int(-8, 6, -2), 3, 2, 3, PlayerVoxelShape.Sphere, Armor, unit, ref seed);

            Add(mesh, new Vector3Int(-1, 12, -2), 2, 1, 2, PlayerVoxelShape.Box, Skin, unit, ref seed);

            // Grosse tête chibi (~40 % de la hauteur).
            const int headSize = 11;
            var headOrigin = new Vector3Int(-5, 13, -5);
            AddHead(mesh, headOrigin, headSize, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 13, -6), 12, 9, 12, PlayerVoxelShape.Dome, Hair, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 21, -4), 4, 2, 3, PlayerVoxelShape.Box, Hair, unit, ref seed);
            PlayerVoxelCuteFace.AddSparkles(mesh, headOrigin, headSize, unit, ref seed);

            // Épée dans le dos.
            Add(mesh, new Vector3Int(-1, 8, 3), 2, 8, 1, PlayerVoxelShape.Box, Blade, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 7, 3), 4, 2, 2, PlayerVoxelShape.Box, Hilt, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 7, 3), 2, 1, 1, PlayerVoxelShape.Box, ArmorLight, unit, ref seed);

            return mesh.ToMesh("PlayerVoxelModel_Swordsman");
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
