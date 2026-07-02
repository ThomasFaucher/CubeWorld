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
                VoxelType.Grass => new Color32(96, 170, 66, 255),
                VoxelType.Dirt => new Color32(126, 88, 60, 255),
                VoxelType.Stone => new Color32(130, 134, 138, 255),
                VoxelType.Sand => new Color32(226, 205, 132, 255),
                VoxelType.Snow => new Color32(238, 244, 250, 255),
                VoxelType.Water => new Color32(58, 126, 204, 255),
                _ => new Color32(255, 0, 255, 255), // magenta = type inconnu, visible en debug
            };

            // L'herbe varie plus que le reste (prairies chatoyantes façon CubeWorld),
            // l'eau reste parfaitement uniforme.
            float strength = type switch
            {
                VoxelType.Grass => 0.10f,
                VoxelType.Water => 0f,
                _ => 0.05f,
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
