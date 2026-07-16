using System;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Core
{
    /// <summary>
    /// Tamponne des primitives (<see cref="VoxelShape"/>) dans une <see cref="VoxelGrid"/>.
    /// Reprend les équations de masque de l'ancien PlayerVoxelMeshCore.BuildMask, mais écrit
    /// des couleurs dans la grille au lieu de générer des faces : le meshing est fait plus
    /// tard, une seule fois pour toute la pièce.
    /// </summary>
    internal static class VoxelStamper
    {
        private const float DomeCutoff = 0.30f;

        public static void Stamp(
            VoxelGrid grid,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            VoxelShape shape,
            Color32 color
        ) => Stamp(grid, origin, sx, sy, sz, shape, (_, _, _) => color);

        /// <summary>
        /// Variante avec couleur par voxel (coordonnées locales au tampon) : utile pour peindre
        /// des motifs (visage, emblème) directement pendant le remplissage.
        /// </summary>
        public static void Stamp(
            VoxelGrid grid,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            VoxelShape shape,
            Func<int, int, int, Color32> colorAt
        )
        {
            float cx = (sx - 1) / 2f;
            float cy = (sy - 1) / 2f;
            float cz = (sz - 1) / 2f;
            float rx = sx / 2f;
            float ry = sy / 2f;
            float rz = sz / 2f;

            for (int x = 0; x < sx; x++)
            {
                for (int y = 0; y < sy; y++)
                {
                    for (int z = 0; z < sz; z++)
                    {
                        if (!Contains(shape, x, y, z, cx, cy, cz, rx, ry, rz, sy))
                        {
                            continue;
                        }

                        grid.Set(origin.x + x, origin.y + y, origin.z + z, colorAt(x, y, z));
                    }
                }
            }
        }

        private static bool Contains(
            VoxelShape shape,
            int x,
            int y,
            int z,
            float cx,
            float cy,
            float cz,
            float rx,
            float ry,
            float rz,
            int sy
        )
        {
            float sphereDistance = Sq((x - cx) / rx) + Sq((y - cy) / ry) + Sq((z - cz) / rz);

            return shape switch
            {
                VoxelShape.Box => true,
                VoxelShape.Column => Sq((x - cx) / rx) + Sq((z - cz) / rz) <= 0.82f,
                VoxelShape.Sphere => sphereDistance <= 0.92f,
                VoxelShape.Dome => y >= sy * DomeCutoff && sphereDistance <= 0.92f,
                VoxelShape.ConeUp => InCone(x, y, z, cx, cz, rx, rz, sy, fromTop: false),
                VoxelShape.ConeDown => InCone(x, y, z, cx, cz, rx, rz, sy, fromTop: true),
                _ => true,
            };
        }

        /// <summary>Coupe conique : pleine à la base, se resserre jusqu'à une pointe centrale.</summary>
        private static bool InCone(
            int x,
            int y,
            int z,
            float cx,
            float cz,
            float rx,
            float rz,
            int sy,
            bool fromTop
        )
        {
            float layer = sy <= 1 ? 0f : (float)y / (sy - 1);
            float t = fromTop ? layer : 1f - layer;
            t = Mathf.Max(t, 0.12f);

            return Sq((x - cx) / (rx * t)) + Sq((z - cz) / (rz * t)) <= 0.9f;
        }

        private static float Sq(float v) => v * v;
    }
}
