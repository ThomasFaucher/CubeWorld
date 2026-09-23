using Unity.Jobs;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Planifie la construction du mesh d'un chunk en tâche de fond (Burst,
    /// voir <see cref="ChunkMeshBuildJob"/>). Le job ne démarre qu'une fois la
    /// génération du chunk lui-même et celle de ses 6 voisins directs déjà
    /// chargés terminées, pour ne jamais lire un tableau de voxels encore en
    /// cours d'écriture. Produit deux meshes séparés : <paramref name="opaqueOutput"/>
    /// (terrain, matériau opaque) et <paramref name="waterOutput"/> (matériau
    /// transparent), car ils nécessitent des passes de rendu différentes.
    /// Produit aussi <paramref name="foliageOutput"/> (touffes d'herbe, mesh à
    /// part sans collider — voir <see cref="VegetationConfig"/>), sauf si
    /// <paramref name="includeFoliage"/> est faux (LOD lointain, voir
    /// <see cref="WorldConfig.LodNearDistance"/>) : le terrain lui-même garde
    /// toujours le détail complet, seul cet extra est coupé à distance.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        public static JobHandle ScheduleBuild(
            Chunk chunk,
            VoxelWorld world,
            ChunkMeshData opaqueOutput,
            ChunkMeshData waterOutput,
            ChunkMeshData foliageOutput,
            VegetationConfig vegetationConfig,
            bool includeFoliage)
        {
            int3 coord = chunk.Coord;
            BiomeSampleParams biome = world.BiomeParams;

            var job = new ChunkMeshBuildJob
            {
                Voxels = chunk.Voxels,
                NeighborBack = world.GetNeighborVoxels(coord, new int3(0, 0, -1)),
                NeighborFront = world.GetNeighborVoxels(coord, new int3(0, 0, 1)),
                NeighborTop = world.GetNeighborVoxels(coord, new int3(0, 1, 0)),
                NeighborBottom = world.GetNeighborVoxels(coord, new int3(0, -1, 0)),
                NeighborLeft = world.GetNeighborVoxels(coord, new int3(-1, 0, 0)),
                NeighborRight = world.GetNeighborVoxels(coord, new int3(1, 0, 0)),
                Size = chunk.Size,
                Origin = chunk.WorldOrigin,
                OpaqueVertices = opaqueOutput.Vertices,
                OpaqueNormals = opaqueOutput.Normals,
                OpaqueColors = opaqueOutput.Colors,
                OpaqueTriangles = opaqueOutput.Triangles,
                WaterVertices = waterOutput.Vertices,
                WaterNormals = waterOutput.Normals,
                WaterColors = waterOutput.Colors,
                WaterTriangles = waterOutput.Triangles,
                FoliageVertices = foliageOutput.Vertices,
                FoliageNormals = foliageOutput.Normals,
                FoliageColors = foliageOutput.Colors,
                FoliageTriangles = foliageOutput.Triangles,
                GrassTuftDensity = vegetationConfig.GrassTuftDensity,
                GrassTuftMinSize = vegetationConfig.GrassTuftMinSize,
                GrassTuftMaxSize = vegetationConfig.GrassTuftMaxSize,
                GrassTuftMinWidth = vegetationConfig.GrassTuftMinWidth,
                GrassTuftMaxWidth = vegetationConfig.GrassTuftMaxWidth,
                FlowerDensity = vegetationConfig.FlowerDensity,
                FlowerMinSize = vegetationConfig.FlowerMinSize,
                FlowerMaxSize = vegetationConfig.FlowerMaxSize,
                FlowerMinWidth = vegetationConfig.FlowerMinWidth,
                FlowerMaxWidth = vegetationConfig.FlowerMaxWidth,
                TemperatureSeedOffset = biome.TemperatureSeedOffset,
                HumiditySeedOffset = biome.HumiditySeedOffset,
                BiomeNoiseScale = biome.NoiseScale,
                BiomeOctaves = biome.Octaves,
                SnowTemperatureThreshold = biome.SnowTemperatureThreshold,
                DesertTemperatureThreshold = biome.DesertTemperatureThreshold,
                DesertHumidityThreshold = biome.DesertHumidityThreshold,
                SwampHumidityThreshold = biome.SwampHumidityThreshold,
                ForestHumidityThreshold = biome.ForestHumidityThreshold,
                IncludeFoliage = includeFoliage,
            };

            JobHandle dependency = JobHandle.CombineDependencies(
                world.GetGenerationHandle(coord),
                world.CombineNeighborGenerationHandles(coord));

            return job.Schedule(dependency);
        }
    }
}
