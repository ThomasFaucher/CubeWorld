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
    /// </summary>
    public static class ChunkMeshBuilder
    {
        public static JobHandle ScheduleBuild(Chunk chunk, VoxelWorld world, ChunkMeshData opaqueOutput, ChunkMeshData waterOutput)
        {
            int3 coord = chunk.Coord;

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
            };

            JobHandle dependency = JobHandle.CombineDependencies(
                world.GetGenerationHandle(coord),
                world.CombineNeighborGenerationHandles(coord));

            return job.Schedule(dependency);
        }
    }
}
