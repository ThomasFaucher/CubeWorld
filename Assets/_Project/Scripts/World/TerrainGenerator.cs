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

        private const int NoiseOctaves = 4;

        // Bruit de biome : moins d'octaves que le relief (des taches lisses, pas de
        // détail fin) ; seuils de classification température/humidité (§BiomeShape).
        private const int BiomeNoiseOctaves = 2;
        private const float SnowTemperatureThreshold = 0.35f;
        private const float DesertTemperatureThreshold = 0.65f;
        private const float DesertHumidityThreshold = 0.4f;
        private const float SwampHumidityThreshold = 0.7f;
        private const float ForestHumidityThreshold = 0.55f;

        private readonly WorldConfig config;
        private readonly float2 seedOffset;
        private readonly float2 riverSeedOffset;
        private readonly float2 basinSeedOffset;
        private readonly float2 entranceSeedOffset;
        private readonly BiomeSampleParams biomeParams;

        public TerrainGenerator(WorldConfig config)
        {
            this.config = config;

            // La graine décale les coordonnées échantillonnées dans le bruit :
            // deux graines différentes produisent deux mondes différents.
            var rng = new Random(math.max(1u, (uint)config.Seed));
            seedOffset = rng.NextFloat2(-10000f, 10000f);
            riverSeedOffset = rng.NextFloat2(-10000f, 10000f);
            basinSeedOffset = rng.NextFloat2(-10000f, 10000f);
            entranceSeedOffset = rng.NextFloat2(-10000f, 10000f);
            biomeParams = new BiomeSampleParams(
                rng.NextFloat2(-10000f, 10000f),
                rng.NextFloat2(-10000f, 10000f),
                config.BiomeNoiseScale,
                BiomeNoiseOctaves,
                SnowTemperatureThreshold,
                DesertTemperatureThreshold,
                DesertHumidityThreshold,
                SwampHumidityThreshold,
                ForestHumidityThreshold);
        }

        /// <summary>Même paramètres biome que le job de génération — pour teintes au meshing.</summary>
        public BiomeSampleParams BiomeParams => biomeParams;

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
                DirtDepth = DirtDepth,
                RiverSeedOffset = riverSeedOffset,
                BasinSeedOffset = basinSeedOffset,
                EntranceSeedOffset = entranceSeedOffset,
                TemperatureSeedOffset = biomeParams.TemperatureSeedOffset,
                HumiditySeedOffset = biomeParams.HumiditySeedOffset,
                BiomeNoiseScale = biomeParams.NoiseScale,
                BiomeOctaves = biomeParams.Octaves,
                SnowTemperatureThreshold = biomeParams.SnowTemperatureThreshold,
                DesertTemperatureThreshold = biomeParams.DesertTemperatureThreshold,
                DesertHumidityThreshold = biomeParams.DesertHumidityThreshold,
                SwampHumidityThreshold = biomeParams.SwampHumidityThreshold,
                ForestHumidityThreshold = biomeParams.ForestHumidityThreshold,
            };

            int columnCount = chunk.Size * chunk.Size;
            const int columnsPerBatch = 8;
            return job.Schedule(columnCount, columnsPerBatch, dependency);
        }

        /// <summary>
        /// Hauteur de la surface en (x, z) monde, en voxels (échantillon ponctuel hors job,
        /// utilisé par le spawn du joueur et la végétation) — reflète le même relief par
        /// biome et les mêmes rivières/lacs que le job de génération, pour rester cohérent
        /// avec ce que verra effectivement le joueur à cet endroit.
        /// </summary>
        public int SampleHeight(int worldX, int worldZ)
        {
            BiomeWeights weights = biomeParams.SampleWeights(worldX, worldZ);
            int surfaceHeight = TerrainShape.SampleHeight(
                worldX,
                worldZ,
                seedOffset,
                config.NoiseScale,
                NoiseOctaves,
                config.MaxTerrainHeight,
                weights,
                config.SeaLevel);

            return RiverShape.ApplyRivers(worldX, worldZ, surfaceHeight, riverSeedOffset, basinSeedOffset, config.SeaLevel);
        }

        /// <summary>Biome de la colonne (x, z) monde (échantillon ponctuel hors job — utilisé par la végétation).</summary>
        public BiomeType SampleBiome(int worldX, int worldZ)
        {
            return biomeParams.Sample(worldX, worldZ);
        }
    }
}
