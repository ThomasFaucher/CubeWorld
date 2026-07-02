using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Génère le terrain d'un chunk à partir de bruit de Perlin fractal (fBm) :
    /// une carte de hauteur 2D, puis des couches (herbe/sable/neige en surface,
    /// terre en dessous, pierre en profondeur, eau jusqu'au niveau de la mer).
    /// </summary>
    public sealed class TerrainGenerator
    {
        // Épaisseur de la couche de terre sous la surface, en voxels.
        private const int DirtDepth = 4;

        // Au-dessus de cette fraction de la hauteur max, la surface est enneigée.
        private const float SnowRatio = 0.8f;

        private const int NoiseOctaves = 4;

        private readonly WorldConfig config;
        private readonly float2 seedOffset;
        private readonly int snowHeight;

        public TerrainGenerator(WorldConfig config)
        {
            this.config = config;
            snowHeight = (int)(config.MaxTerrainHeight * SnowRatio);

            // La graine décale les coordonnées échantillonnées dans le bruit :
            // deux graines différentes produisent deux mondes différents.
            var rng = new Random(math.max(1u, (uint)config.Seed));
            seedOffset = rng.NextFloat2(-10000f, 10000f);
        }

        /// <summary>Remplit tous les voxels du chunk selon la carte de hauteur.</summary>
        public void Generate(Chunk chunk)
        {
            int size = chunk.Size;
            int3 origin = chunk.WorldOrigin;

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int surfaceHeight = SampleHeight(origin.x + x, origin.z + z);
                    for (int y = 0; y < size; y++)
                    {
                        chunk.SetVoxel(x, y, z, CreateVoxel(origin.y + y, surfaceHeight));
                    }
                }
            }
        }

        /// <summary>Hauteur de la surface en (x, z) monde, en voxels.</summary>
        public int SampleHeight(int worldX, int worldZ)
        {
            float2 point = (new float2(worldX, worldZ) + seedOffset) * config.NoiseScale;
            return (int)(Fbm(point, NoiseOctaves) * config.MaxTerrainHeight);
        }

        // Bruit fractal : somme d'octaves de Perlin, chacune deux fois plus fine
        // et deux fois plus faible que la précédente. Résultat dans [0, 1].
        private static float Fbm(float2 point, int octaves)
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

        private Voxel CreateVoxel(int worldY, int surfaceHeight)
        {
            // Au-dessus de la surface : de l'eau jusqu'au niveau de la mer, sinon de l'air.
            if (worldY > surfaceHeight)
            {
                return worldY <= config.SeaLevel
                    ? new Voxel(VoxelType.Water)
                    : Voxel.Air;
            }

            // Voxel de surface : sable près de l'eau, neige en altitude, herbe sinon.
            if (worldY == surfaceHeight)
            {
                if (worldY <= config.SeaLevel + 1)
                {
                    return new Voxel(VoxelType.Sand);
                }

                return worldY >= snowHeight
                    ? new Voxel(VoxelType.Snow)
                    : new Voxel(VoxelType.Grass);
            }

            // Sous la surface : une couche de terre, puis de la pierre.
            return worldY > surfaceHeight - DirtDepth
                ? new Voxel(VoxelType.Dirt)
                : new Voxel(VoxelType.Stone);
        }
    }
}
