using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Palette de couleurs des voxels — style CubeWorld : une couleur unie par
    /// type (pas de textures), teintée par biome, avec une variation régionale
    /// douce (pas de damier voxel-par-voxel).
    /// </summary>
    public static class VoxelPalette
    {
        public static Color32 GetColor(VoxelType type, int3 worldPosition, BiomeType biome)
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
                // Minerais : couleurs franches pour rester repérables dans la pénombre d'une
                // grotte (voir CaveShape) malgré l'éclairage en paliers du shader.
                VoxelType.OreCopper => new Color32(196, 110, 68, 255),
                VoxelType.OreIron => new Color32(101, 112, 130, 255),
                VoxelType.OreGold => new Color32(255, 208, 66, 255),
                _ => new Color32(255, 0, 255, 255), // magenta = type inconnu, visible en debug
            };

            baseColor = ApplyBiomeTint(baseColor, type, biome);
            return ApplyRegionalTint(baseColor, type, worldPosition);
        }

        // Multiplicateurs RGB par biome : différencient Plaines / Forêt / Désert / Neige
        // sans changer de matériau (toujours des vertex colors).
        private static Color32 ApplyBiomeTint(Color32 color, VoxelType type, BiomeType biome)
        {
            float3 mul = biome switch
            {
                BiomeType.Plains => type switch
                {
                    VoxelType.Grass => new float3(1.12f, 1.05f, 0.82f), // jaune-vert chaud
                    VoxelType.Dirt => new float3(1.05f, 1.0f, 0.92f),
                    _ => new float3(1f, 1f, 1f),
                },
                BiomeType.Forest => type switch
                {
                    VoxelType.Grass => new float3(0.78f, 0.95f, 0.72f), // vert plus profond
                    VoxelType.Dirt => new float3(0.88f, 0.85f, 0.82f),
                    VoxelType.Stone => new float3(0.92f, 0.96f, 0.94f),
                    _ => new float3(1f, 1f, 1f),
                },
                BiomeType.Desert => type switch
                {
                    VoxelType.Sand => new float3(1.08f, 0.96f, 0.78f), // sable plus chaud
                    VoxelType.Stone => new float3(1.08f, 0.98f, 0.88f), // pierre ocrée
                    VoxelType.Dirt => new float3(1.1f, 0.95f, 0.8f),
                    _ => new float3(1f, 1f, 1f),
                },
                BiomeType.Snow => type switch
                {
                    VoxelType.Snow => new float3(0.92f, 0.96f, 1.08f), // neige bleutée
                    VoxelType.Ice => new float3(0.9f, 0.96f, 1.1f),
                    VoxelType.Stone => new float3(0.88f, 0.92f, 1.05f), // pierre froide
                    VoxelType.Dirt => new float3(0.9f, 0.92f, 1.0f),
                    _ => new float3(1f, 1f, 1f),
                },
                BiomeType.Swamp => type switch
                {
                    // Olive très sombre / boueux — le marais ne doit pas lire « prairie ».
                    VoxelType.Grass => new float3(0.38f, 0.48f, 0.32f),
                    VoxelType.Dirt => new float3(0.55f, 0.52f, 0.42f),
                    VoxelType.Water => new float3(0.4f, 0.75f, 0.5f), // eau verdâtre sombre
                    VoxelType.Stone => new float3(0.75f, 0.8f, 0.7f),
                    _ => new float3(1f, 1f, 1f),
                },
                _ => new float3(1f, 1f, 1f),
            };

            return ScaleColor(color, mul);
        }

        // Variation régionale continue (bruit lisse) : les blocs voisins restent
        // proches en teinte — plus de damier « un bloc sur deux ».
        private static Color32 ApplyRegionalTint(Color32 color, VoxelType type, int3 worldPosition)
        {
            if (type == VoxelType.Water || type == VoxelType.Air)
            {
                return color;
            }

            float strength = type switch
            {
                VoxelType.Grass => 0.045f,
                VoxelType.Sand => 0.04f,
                VoxelType.Snow => 0.03f,
                _ => 0.02f,
            };

            // ~25 voxels de période : grandes taches douces, pas de grain voxel.
            float2 uv = new float2(worldPosition.x, worldPosition.z) * 0.04f;
            float n = noise.snoise(uv);
            float factor = 1f + (n * strength);

            // Légère dérive de teinte (pas seulement luminosité) pour l'herbe/sable.
            float3 chroma = type switch
            {
                VoxelType.Grass => new float3(1f + (n * 0.02f), 1f, 1f - (n * 0.015f)),
                VoxelType.Sand => new float3(1f + (n * 0.015f), 1f, 1f - (n * 0.02f)),
                _ => new float3(1f, 1f, 1f),
            };

            return ScaleColor(color, new float3(factor, factor, factor) * chroma);
        }

        private static Color32 ScaleColor(Color32 color, float3 mul)
        {
            return new Color32(
                (byte)math.clamp((int)(color.r * mul.x), 0, 255),
                (byte)math.clamp((int)(color.g * mul.y), 0, 255),
                (byte)math.clamp((int)(color.b * mul.z), 0, 255),
                color.a);
        }
    }
}
