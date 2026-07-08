using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Visages chibi expressifs partagés (yeux, joues, bouche).</summary>
    internal static class PlayerVoxelCuteFace
    {
        private static readonly Color32 EyeWhite = new(250, 250, 252, 255);
        private static readonly Color32 Pupil = new(28, 28, 36, 255);
        private static readonly Color32 Blush = new(240, 150, 155, 255);
        private static readonly Color32 Mouth = new(180, 90, 95, 255);

        internal static Color32 PaintSphere(
            int x,
            int y,
            int z,
            int headSize,
            Color32 skin,
            Color32 hair,
            Color32 iris,
            bool hairOnTop = true
        )
        {
            if (hairOnTop && y >= headSize - 3)
            {
                return hair;
            }

            float cx = (headSize - 1) / 2f;
            int frontZ = headSize - 1;

            if (z >= frontZ - 1)
            {
                if (y == 3)
                {
                    if (Mathf.Abs(x - (cx - 2f)) < 0.75f || Mathf.Abs(x - (cx + 2f)) < 0.75f)
                    {
                        if (z == frontZ && Mathf.Abs(x - (cx - 2f)) < 0.35f)
                        {
                            return Pupil;
                        }

                        if (z == frontZ && Mathf.Abs(x - (cx + 2f)) < 0.35f)
                        {
                            return Pupil;
                        }

                        return z == frontZ ? iris : EyeWhite;
                    }
                }

                if (y == 2 && z == frontZ - 1 && (Mathf.Abs(x - (cx - 3f)) < 0.5f || Mathf.Abs(x - (cx + 3f)) < 0.5f))
                {
                    return Blush;
                }

                if (y == 1 && z == frontZ && Mathf.Abs(x - cx) < 1.1f)
                {
                    return Mouth;
                }
            }

            return skin;
        }

        internal static void AddSparkles(
            PlayerVoxelMeshData mesh,
            Vector3Int headOrigin,
            int headSize,
            float unit,
            ref int seed
        )
        {
            float cx = (headSize - 1) / 2f;
            int y = headOrigin.y + 3;
            int z = headOrigin.z + headSize - 1;

            AddDot(mesh, new Vector3Int(Mathf.RoundToInt(headOrigin.x + cx - 2f + 0.5f), y + 1, z), EyeWhite, unit, ref seed);
            AddDot(mesh, new Vector3Int(Mathf.RoundToInt(headOrigin.x + cx + 2f + 0.5f), y + 1, z), EyeWhite, unit, ref seed);
        }

        private static void AddDot(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            Color32 color,
            float unit,
            ref int seed
        ) => PlayerVoxelMeshCore.AddPart(mesh, origin, 1, 1, 1, PlayerVoxelShape.Box, color, unit, seed++);
    }
}
