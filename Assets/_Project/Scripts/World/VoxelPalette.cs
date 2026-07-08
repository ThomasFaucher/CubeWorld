using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Palette de couleurs des voxels — style CubeWorld : une couleur unie par
    /// type (pas de textures), avec une légère variation aléatoire par voxel
    /// pour casser l'uniformité des grandes surfaces.
    /// </summary>
    public static class VoxelPalette
    {
        public static Color32 GetColor(VoxelType type, int3 worldPosition)
        {
            Color32 baseColor = type switch
            {
                // Palette plus saturée/vive (façon cartoon) que des teintes
                // terreuses réalistes : le but est que la couleur "pop" même
                // une fois passée dans l'éclairage en paliers du shader.
                VoxelType.Grass => new Color32(74, 197, 58, 255),
                VoxelType.Dirt => new Color32(151, 96, 51, 255),
                VoxelType.Stone => new Color32(142, 147, 153, 255),
                VoxelType.Sand => new Color32(241, 201, 90, 255),
                VoxelType.Snow => new Color32(255, 255, 255, 255),
                // Blanc bleuté, distinct du blanc pur de Snow : surface d'eau
                // gelée en biome Neige (voir TerrainShape.CreateVoxel).
                VoxelType.Ice => new Color32(214, 232, 245, 255),
                // Alpha réduite : rendue avec le shader transparent CubeWorld/VoxelWater.
                VoxelType.Water => new Color32(31, 143, 214, 180),
                _ => new Color32(255, 0, 255, 255), // magenta = type inconnu, visible en debug
            };

            // Variation légère seulement : le style cartoon visé veut des
            // couleurs unies, le relief se lit via l'éclairage en paliers du
            // shader (CubeWorld/VoxelTerrain), pas via le bruit de couleur.
            float strength = type switch
            {
                VoxelType.Grass => 0.025f,
                VoxelType.Water => 0f,
                _ => 0.015f,
            };

            return Vary(baseColor, worldPosition, strength);
        }

        // Assombrit/éclaircit la couleur d'un facteur déterministe dérivé de la
        // position : le même voxel garde la même teinte d'une frame à l'autre.
        private static Color32 Vary(Color32 color, int3 position, float strength)
        {
            if (strength <= 0f)
            {
                return color;
            }

            uint hash = math.hash(position);
            float factor = 1f + ((hash & 1023) / 1023f * 2f - 1f) * strength;

            return new Color32(
                (byte)math.clamp((int)(color.r * factor), 0, 255),
                (byte)math.clamp((int)(color.g * factor), 0, 255),
                (byte)math.clamp((int)(color.b * factor), 0, 255),
                color.a);
        }
    }
}
