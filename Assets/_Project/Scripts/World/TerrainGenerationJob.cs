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
        public int SnowHeight;
        public int DirtDepth;

        public float2 TemperatureSeedOffset;
        public float2 HumiditySeedOffset;
        public float BiomeNoiseScale;
        public int BiomeOctaves;
        public float SnowTemperatureThreshold;
        public float DesertTemperatureThreshold;
        public float DesertHumidityThreshold;
        public float ForestHumidityThreshold;

        public void Execute(int columnIndex)
        {
            int x = columnIndex % Size;
            int z = columnIndex / Size;

            int surfaceHeight = TerrainShape.SampleHeight(Origin.x + x, Origin.z + z, SeedOffset, NoiseScale, Octaves, MaxTerrainHeight);
            BiomeType biome = BiomeShape.Sample(
                Origin.x + x,
                Origin.z + z,
                TemperatureSeedOffset,
                HumiditySeedOffset,
                BiomeNoiseScale,
                BiomeOctaves,
                SnowTemperatureThreshold,
                DesertTemperatureThreshold,
                DesertHumidityThreshold,
                ForestHumidityThreshold);

            for (int y = 0; y < Size; y++)
            {
                Voxel voxel = TerrainShape.CreateVoxel(Origin.y + y, surfaceHeight, SeaLevel, SnowHeight, DirtDepth, biome);
                Voxels[x + Size * (y + Size * z)] = voxel;
            }
        }
    }
}
