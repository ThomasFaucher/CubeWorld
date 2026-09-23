using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Job Burst qui remplit les voxels d'un chunk. Une itération traite une
    /// colonne (x, z) entière : la hauteur de surface n'est calculée qu'une fois
    /// par colonne, puis chaque voxel de la colonne en est déduit.
    /// </summary>
    [BurstCompile]
    internal struct TerrainGenerationJob : IJobParallelFor
    {
        // Une itération remplit toute une colonne en Y ; les colonnes ne se
        // chevauchent jamais, mais le tableau est plus grand que le nombre
        // d'itérations (size² colonnes pour size³ voxels), d'où ce contournement
        // explicite de la vérification d'aliasing par défaut de IJobParallelFor.
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Voxel> Voxels;

        public int Size;
        public int3 Origin;

        public float2 SeedOffset;
        public float NoiseScale;
        public int Octaves;
        public int MaxTerrainHeight;
        public int SeaLevel;
        public int DirtDepth;

        public float2 RiverSeedOffset;
        public float2 BasinSeedOffset;
        public float2 EntranceSeedOffset;

        public float2 TemperatureSeedOffset;
        public float2 HumiditySeedOffset;
        public float BiomeNoiseScale;
        public int BiomeOctaves;
        public float SnowTemperatureThreshold;
        public float DesertTemperatureThreshold;
        public float DesertHumidityThreshold;
        public float SwampHumidityThreshold;
        public float ForestHumidityThreshold;

        public void Execute(int columnIndex)
        {
            int x = columnIndex % Size;
            int z = columnIndex / Size;
            int worldX = Origin.x + x;
            int worldZ = Origin.z + z;

            // Un seul échantillon de climat, réutilisé pour la classification dure
            // (matériau/props) ET les poids lissés (relief, voir BiomeHeightProfile) — pas
            // de double bruit pour la même colonne. Le relief mélange désormais un profil
            // par biome (dunes/pics/ondulations/aplatissement) via ces poids continus, ce
            // qui évite la falaise qu'aurait créée une ancienne tentative de dépression
            // dure par biome (voir BiomeHeightProfile.Swamp).
            BiomeShape.SampleClimate(
                worldX,
                worldZ,
                TemperatureSeedOffset,
                HumiditySeedOffset,
                BiomeNoiseScale,
                BiomeOctaves,
                out float temperature,
                out float humidity);

            BiomeType biome = BiomeShape.Classify(
                temperature,
                humidity,
                SnowTemperatureThreshold,
                DesertTemperatureThreshold,
                DesertHumidityThreshold,
                SwampHumidityThreshold,
                ForestHumidityThreshold);

            BiomeWeights biomeWeights = BiomeShape.ClassifyWeights(temperature, humidity);

            int surfaceHeight = TerrainShape.SampleHeight(
                worldX,
                worldZ,
                SeedOffset,
                NoiseScale,
                Octaves,
                MaxTerrainHeight,
                biomeWeights,
                SeaLevel);

            // Rivières et lacs : creusent localement sous le niveau de la mer, qui se
            // remplit alors d'eau via la règle de flood déjà appliquée par CreateVoxel.
            surfaceHeight = RiverShape.ApplyRivers(worldX, worldZ, surfaceHeight, RiverSeedOffset, BasinSeedOffset, SeaLevel);

            // Entrée de grotte (doline) : une seule fois par colonne — le puits force
            // de l'air quelle que soit la couche (herbe/terre/pierre).
            bool hasEntrance = CaveEntranceShape.TryGetShaft(
                worldX,
                worldZ,
                surfaceHeight,
                EntranceSeedOffset,
                SeedOffset,
                SeaLevel,
                out int shaftBottomY,
                out float2 entranceCenter);

            for (int y = 0; y < Size; y++)
            {
                int worldY = Origin.y + y;
                Voxel voxel = TerrainShape.CreateVoxel(
                    worldY,
                    surfaceHeight,
                    SeaLevel,
                    DirtDepth,
                    biome);

                // Grottes/minerai : seule la Pierre profonde est concernée (jamais la
                // surface/Terre/Sable/Eau — voir CaveShape).
                if (voxel.Type == VoxelType.Stone)
                {
                    if (CaveShape.IsCave(worldX, worldY, worldZ, surfaceHeight, SeedOffset))
                    {
                        voxel = Voxel.Air;
                    }
                    else
                    {
                        VoxelType oreType = CaveShape.ApplyOreVein(worldX, worldY, worldZ, surfaceHeight, SeedOffset);
                        if (oreType != VoxelType.Stone)
                        {
                            voxel = new Voxel(oreType);
                        }
                    }
                }

                // Puits d'entrée : override final — perce herbe/terre/pierre jusqu'à
                // la poche de grotte (ou profondeur de repli).
                if (hasEntrance
                    && CaveEntranceShape.IsInsideShaft(
                        worldX,
                        worldY,
                        worldZ,
                        surfaceHeight,
                        shaftBottomY,
                        entranceCenter))
                {
                    voxel = Voxel.Air;
                }

                Voxels[x + Size * (y + Size * z)] = voxel;
            }
        }
    }
}
