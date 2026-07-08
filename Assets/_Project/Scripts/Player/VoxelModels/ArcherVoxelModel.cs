using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Archer chibi : fin mais mignon, capuche, carquois détaillé.</summary>
    internal static class ArcherVoxelModel
    {
        private const int HeightVoxels = 28;

        private static readonly Color32 Skin = new(210, 170, 138, 255);
        private static readonly Color32 Eye = new(60, 120, 70, 255);
        private static readonly Color32 Hood = new(62, 48, 36, 255);
        private static readonly Color32 HoodDark = new(42, 32, 24, 255);
        private static readonly Color32 Tunic = new(58, 98, 52, 255);
        private static readonly Color32 TunicLight = new(78, 128, 68, 255);
        private static readonly Color32 Pants = new(74, 58, 42, 255);
        private static readonly Color32 Leather = new(120, 82, 48, 255);
        private static readonly Color32 Boot = new(52, 40, 32, 255);
        private static readonly Color32 Wood = new(140, 96, 58, 255);
        private static readonly Color32 Feather = new(180, 60, 60, 255);

        public static Mesh Build(float targetHeight)
        {
            float unit = targetHeight / HeightVoxels;
            var mesh = new PlayerVoxelMeshData();
            int seed = 0;

            Add(mesh, new Vector3Int(1, 0, -2), 3, 2, 5, PlayerVoxelShape.Box, Boot, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 0, -2), 3, 2, 5, PlayerVoxelShape.Box, Boot, unit, ref seed);

            // Jambes un peu plus longues que l'épéiste, mais toujours chibi.
            Add(mesh, new Vector3Int(1, 2, -2), 3, 5, 3, PlayerVoxelShape.Column, Pants, unit, ref seed);
            Add(mesh, new Vector3Int(-4, 2, -2), 3, 5, 3, PlayerVoxelShape.Column, Pants, unit, ref seed);

            Add(mesh, new Vector3Int(-4, 7, -3), 8, 1, 5, PlayerVoxelShape.Box, Leather, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 7, -2), 2, 1, 1, PlayerVoxelShape.Box, TunicLight, unit, ref seed);

            Add(mesh, new Vector3Int(-4, 8, -3), 8, 4, 5, PlayerVoxelShape.Box, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-3, 9, -2), 2, 2, 1, PlayerVoxelShape.Box, TunicLight, unit, ref seed);

            Add(mesh, new Vector3Int(4, 9, -2), 2, 3, 2, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 9, -2), 2, 3, 2, PlayerVoxelShape.Column, Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(4, 7, -2), 2, 2, 2, PlayerVoxelShape.Sphere, Skin, unit, ref seed);
            Add(mesh, new Vector3Int(-6, 7, -2), 2, 2, 2, PlayerVoxelShape.Sphere, Skin, unit, ref seed);

            const int headSize = 10;
            var headOrigin = new Vector3Int(-4, 12, -4);
            AddHead(mesh, headOrigin, headSize, unit, ref seed);

            // Capuche avec bord et pompon.
            Add(mesh, new Vector3Int(-5, 12, -6), 10, 8, 10, PlayerVoxelShape.Dome, Hood, unit, ref seed);
            Add(mesh, new Vector3Int(-3, 12, -3), 6, 3, 2, PlayerVoxelShape.Box, HoodDark, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 19, -5), 2, 2, 2, PlayerVoxelShape.Sphere, Feather, unit, ref seed);
            PlayerVoxelCuteFace.AddSparkles(mesh, headOrigin, headSize, unit, ref seed);

            // Carquois + flèches.
            Add(mesh, new Vector3Int(3, 9, 2), 2, 6, 2, PlayerVoxelShape.Box, Leather, unit, ref seed);
            Add(mesh, new Vector3Int(3, 14, 2), 2, 1, 2, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(4, 13, 2), 1, 2, 1, PlayerVoxelShape.Box, Feather, unit, ref seed);

            // Arc compact.
            Add(mesh, new Vector3Int(-1, 10, 2), 1, 6, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(-2, 12, 2), 2, 1, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(0, 12, 2), 2, 1, 1, PlayerVoxelShape.Box, Wood, unit, ref seed);
            Add(mesh, new Vector3Int(-1, 9, 2), 1, 1, 1, PlayerVoxelShape.Box, Leather, unit, ref seed);

            return mesh.ToMesh("PlayerVoxelModel_Archer");
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
                PlayerVoxelCuteFace.PaintSphere(x, y, z, headSize, Skin, HoodDark, Eye, hairOnTop: false);

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
