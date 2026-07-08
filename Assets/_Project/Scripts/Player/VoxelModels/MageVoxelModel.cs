using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Mage chibi : robe bouffante, chapeau pointu, orbe lumineux.</summary>
    internal static class MageVoxelModel
    {
        private const int HeightVoxels = 30;

        private static readonly Color32 Skin = new(228, 198, 178, 255);
        private static readonly Color32 Eye = new(120, 80, 200, 255);
        private static readonly Color32 Robe = new(48, 38, 92, 255);
        private static readonly Color32 RobeDark = new(32, 24, 64, 255);
        private static readonly Color32 RobeLight = new(68, 58, 118, 255);
        private static readonly Color32 Trim = new(180, 150, 60, 255);
        private static readonly Color32 Orb = new(100, 180, 240, 255);
        private static readonly Color32 OrbCore = new(220, 240, 255, 255);
        private static readonly Color32 Staff = new(90, 70, 50, 255);

        public static Mesh Build(float targetHeight)
        {
            float unit = targetHeight / HeightVoxels;
            var mesh = new PlayerVoxelMeshData();
            int seed = 0;

            // Robe bouffante (jambes cachées).
            Add(mesh, new Vector3Int(-5, 0, -3), 10, 14, 6, PlayerVoxelShape.Column, Robe, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 0, -4), 12, 2, 8, PlayerVoxelShape.Box, RobeDark, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 2, -4), 12, 1, 8, PlayerVoxelShape.Box, Trim, unit, ref seed);

            Add(mesh, new Vector3Int(-5, 12, -3), 10, 1, 6, PlayerVoxelShape.Box, Trim, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 12, -2), 2, 1, 1, PlayerVoxelShape.Box, RobeLight, unit, ref seed);

            // Col + manches bouffantes.
            Add(mesh, new Vector3Int(-3, 13, -2), 6, 2, 4, PlayerVoxelShape.Box, RobeDark, unit, ref seed);
            Add(mesh, new Vector3Int(5, 11, -2), 3, 4, 3, PlayerVoxelShape.Box, Robe, unit, ref seed);
            Add(mesh, new Vector3Int(-8, 11, -2), 3, 4, 3, PlayerVoxelShape.Box, Robe, unit, ref seed);
            Add(mesh, new Vector3Int(5, 9, -2), 2, 2, 2, PlayerVoxelShape.Sphere, RobeLight, unit, ref seed);
            Add(mesh, new Vector3Int(-7, 9, -2), 2, 2, 2, PlayerVoxelShape.Sphere, RobeLight, unit, ref seed);

            const int headSize = 10;
            var headOrigin = new Vector3Int(-4, 14, -4);
            AddHead(mesh, headOrigin, headSize, unit, ref seed);
            PlayerVoxelCuteFace.AddSparkles(mesh, headOrigin, headSize, unit, ref seed);

            // Chapeau pointu empilé avec bande dorée.
            Add(mesh, new Vector3Int(-6, 23, -6), 12, 2, 12, PlayerVoxelShape.Box, Robe, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 23, -6), 12, 1, 12, PlayerVoxelShape.Box, Trim, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 25, -4), 8, 2, 8, PlayerVoxelShape.Box, Robe, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 27, -2), 4, 2, 4, PlayerVoxelShape.Box, RobeDark, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 29, -1), 2, 1, 2, PlayerVoxelShape.Box, Trim, unit, ref seed);

            // Bâton + orbe scintillant.
            Add(mesh, new Vector3Int(0, 5, 3), 1, 22, 1, PlayerVoxelShape.Box, Staff, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 26, 2), 3, 3, 3, PlayerVoxelShape.Sphere, Orb, unit, ref seed);
            Add(mesh, new Vector3Int(0, 27, 3), 1, 1, 1, PlayerVoxelShape.Box, OrbCore, unit, ref seed);

            return mesh.ToMesh("PlayerVoxelModel_Mage");
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
                PlayerVoxelCuteFace.PaintSphere(x, y, z, headSize, Skin, RobeDark, Eye, hairOnTop: false);

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
