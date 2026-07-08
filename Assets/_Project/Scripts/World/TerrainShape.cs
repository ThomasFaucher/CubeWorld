using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Fonctions pures de forme du terrain (bruit fractal, choix du voxel selon
    /// la hauteur). Ne dépend d'aucun objet managé : appelable aussi bien depuis
    /// <see cref="TerrainGenerator.SampleHeight"/> (thread principal) que depuis
    /// <see cref="TerrainGenerationJob"/> (job Burst).
    /// </summary>
    internal static class TerrainShape
    {
        /// <summary>Hauteur de la surface en (x, z) monde, en voxels.</summary>
        public static int SampleHeight(int worldX, int worldZ, float2 seedOffset, float noiseScale, int octaves, int maxTerrainHeight)
        {
            float2 point = (new float2(worldX, worldZ) + seedOffset) * noiseScale;
            return (int)(Fbm(point, octaves) * maxTerrainHeight);
        }

        // Bruit fractal : somme d'octaves de Perlin, chacune deux fois plus fine
        // et deux fois plus faible que la précédente. Résultat dans [0, 1].
        public static float Fbm(float2 point, int octaves)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float totalAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * (noise.cnoise(point * frequency) * 0.5f + 0.5f);
                totalAmplitude += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return sum / totalAmplitude;
        }

        /// <summary>Choisit le voxel à cette hauteur monde selon la hauteur de surface locale et le biome.</summary>
        public static Voxel CreateVoxel(int worldY, int surfaceHeight, int seaLevel, int snowHeight, int dirtDepth, BiomeType biome)
        {
            // Au-dessus de la surface : de l'eau jusqu'au niveau de la mer, sinon de l'air.
            if (worldY > surfaceHeight)
            {
                return worldY <= seaLevel
                    ? new Voxel(VoxelType.Water)
                    : Voxel.Air;
            }

            // Voxel de surface : sable près de l'eau (tous biomes, une côte reste une
            // côte), sinon le biome remplace la règle de hauteur (Désert/Neige) ou la
            // laisse telle quelle (Forêt/Plaines : neige en altitude, herbe sinon).
            if (worldY == surfaceHeight)
            {
                if (worldY <= seaLevel + 1)
                {
                    return new Voxel(VoxelType.Sand);
                }

                return biome switch
                {
                    BiomeType.Desert => new Voxel(VoxelType.Sand),
                    BiomeType.Snow => new Voxel(VoxelType.Snow),
                    _ => worldY >= snowHeight
                        ? new Voxel(VoxelType.Snow)
                        : new Voxel(VoxelType.Grass),
                };
            }

            // Sous la surface : dune de sable homogène en désert, sinon une couche de
            // terre puis de la pierre.
            if (biome == BiomeType.Desert && worldY > surfaceHeight - dirtDepth)
            {
                return new Voxel(VoxelType.Sand);
            }

            return worldY > surfaceHeight - dirtDepth
                ? new Voxel(VoxelType.Dirt)
                : new Voxel(VoxelType.Stone);
        }
    }
}
