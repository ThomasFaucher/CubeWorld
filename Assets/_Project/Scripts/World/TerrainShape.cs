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
        /// <summary>
        /// Hauteur de la surface en (x, z) monde, en voxels : la fraction de bruit de base
        /// est reshapée par biome (dunes, pics, ondulations, aplatissement — voir
        /// <see cref="BiomeHeightProfile"/>) puis mélangée par les poids lissés du point,
        /// pour un relief qui varie selon le biome sans jamais créer de falaise à sa
        /// frontière (bascule dure évitée volontairement, voir TerrainGenerationJob).
        /// </summary>
        public static int SampleHeight(
            int worldX,
            int worldZ,
            float2 seedOffset,
            float noiseScale,
            int octaves,
            int maxTerrainHeight,
            BiomeWeights biomeWeights,
            int seaLevel
        )
        {
            float2 point = (new float2(worldX, worldZ) + seedOffset) * noiseScale;
            float baseFraction = Fbm(point, octaves);
            float seaLevelFraction = (float)seaLevel / maxTerrainHeight;
            float fraction = BiomeHeightProfile.Evaluate(biomeWeights, point, baseFraction, seaLevelFraction);
            return (int)(fraction * maxTerrainHeight);
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
        public static Voxel CreateVoxel(int worldY, int surfaceHeight, int seaLevel, int dirtDepth, BiomeType biome)
        {
            // Au-dessus de la surface : de l'eau jusqu'au niveau de la mer (gelée en
            // biome Neige — un lac n'a pas de raison d'être liquide sous la neige),
            // sinon de l'air.
            if (worldY > surfaceHeight)
            {
                if (worldY <= seaLevel)
                {
                    return biome == BiomeType.Snow
                        ? new Voxel(VoxelType.Ice)
                        : new Voxel(VoxelType.Water);
                }

                return Voxel.Air;
            }

            // Voxel de surface : sable près de l'eau (Désert/Forêt/Plaines — une côte
            // reste une côte), neige près de l'eau en biome Neige (pas de plage
            // sous la neige), sinon le biome décide (Désert -> sable, Neige -> neige,
            // Forêt/Plaines -> toujours herbe, y compris en altitude, voir plus bas).
            if (worldY == surfaceHeight)
            {
                if (worldY <= seaLevel + 1)
                {
                    return biome switch
                    {
                        BiomeType.Snow => new Voxel(VoxelType.Snow),
                        BiomeType.Swamp => new Voxel(VoxelType.Grass),
                        _ => new Voxel(VoxelType.Sand),
                    };
                }

                // Plaines/Forêt : toujours de l'herbe, même en altitude — la neige ne doit
                // apparaître que dans le biome Neige lui-même, pas au sommet de n'importe
                // quelle colline (l'ancienne règle mettait de la neige dès 80% de la hauteur
                // max quel que soit le climat, ce qui neigeait des sommets en plein désert
                // voisin ou en pleine forêt tempérée).
                return biome switch
                {
                    BiomeType.Desert => new Voxel(VoxelType.Sand),
                    BiomeType.Snow => new Voxel(VoxelType.Snow),
                    BiomeType.Swamp => new Voxel(VoxelType.Grass),
                    _ => new Voxel(VoxelType.Grass),
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
