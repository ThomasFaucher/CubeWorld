using Unity.Jobs;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Planifie la génération du terrain d'un chunk en tâche de fond (Burst) :
    /// une carte de hauteur 2D (bruit de Perlin fractal), puis des couches
    /// (herbe/sable/neige en surface, terre en dessous, pierre en profondeur,
    /// eau jusqu'au niveau de la mer). Voir <see cref="TerrainShape"/> pour le
    /// détail des formules, partagées avec le job.
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

        /// <summary>Planifie le remplissage des voxels du chunk sur un thread de fond.</summary>
        public JobHandle ScheduleGenerate(Chunk chunk, JobHandle dependency = default)
        {
            var job = new TerrainGenerationJob
            {
                Voxels = chunk.Voxels,
                Size = chunk.Size,
                Origin = chunk.WorldOrigin,
                SeedOffset = seedOffset,
                NoiseScale = config.NoiseScale,
                Octaves = NoiseOctaves,
                MaxTerrainHeight = config.MaxTerrainHeight,
                SeaLevel = config.SeaLevel,
                SnowHeight = snowHeight,
                DirtDepth = DirtDepth,
            };

            int columnCount = chunk.Size * chunk.Size;
            const int columnsPerBatch = 8;
            return job.Schedule(columnCount, columnsPerBatch, dependency);
        }

        /// <summary>Hauteur de la surface en (x, z) monde, en voxels (échantillon ponctuel hors job).</summary>
        public int SampleHeight(int worldX, int worldZ)
        {
            return TerrainShape.SampleHeight(worldX, worldZ, seedOffset, config.NoiseScale, NoiseOctaves, config.MaxTerrainHeight);
        }
    }
}
