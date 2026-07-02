using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Transforme les voxels d'un chunk en mesh : pour chaque voxel solide, seules
    /// les faces exposées à l'air sont générées (face culling). Chaque face a ses
    /// 4 sommets propres avec une normale de face — c'est ce qui donne le rendu
    /// flat shading. La couleur du voxel est écrite dans les couleurs de vertex.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        // Les 8 coins d'un voxel unitaire, relatifs à son coin (0,0,0).
        private static readonly float3[] Corners =
        {
            new(0f, 0f, 0f), // 0
            new(1f, 0f, 0f), // 1
            new(1f, 1f, 0f), // 2
            new(0f, 1f, 0f), // 3
            new(0f, 0f, 1f), // 4
            new(1f, 0f, 1f), // 5
            new(1f, 1f, 1f), // 6
            new(0f, 1f, 1f), // 7
        };

        // Direction du voisin à tester pour chaque face.
        private static readonly int3[] FaceDirections =
        {
            new(0, 0, -1), // arrière
            new(0, 0, 1),  // avant
            new(0, 1, 0),  // dessus
            new(0, -1, 0), // dessous
            new(-1, 0, 0), // gauche
            new(1, 0, 0),  // droite
        };

        // Les 4 coins de chaque face, ordonnés pour un enroulement horaire
        // (faces visibles de l'extérieur avec le winding Unity).
        private static readonly int[][] FaceCorners =
        {
            new[] { 0, 3, 1, 2 }, // arrière
            new[] { 5, 6, 4, 7 }, // avant
            new[] { 3, 7, 2, 6 }, // dessus
            new[] { 1, 5, 0, 4 }, // dessous
            new[] { 4, 7, 0, 3 }, // gauche
            new[] { 1, 2, 5, 6 }, // droite
        };

        /// <summary>
        /// Construit les données de mesh du chunk. Le <paramref name="world"/>
        /// sert à tester les voxels voisins situés dans les chunks adjacents.
        /// </summary>
        public static ChunkMeshData Build(Chunk chunk, IVoxelLookup world)
        {
            var data = new ChunkMeshData();
            int size = chunk.Size;
            int3 origin = chunk.WorldOrigin;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        Voxel voxel = chunk.GetVoxel(x, y, z);
                        if (voxel.IsAir)
                        {
                            continue;
                        }

                        var local = new int3(x, y, z);
                        AddVisibleFaces(data, chunk, world, voxel, local, origin + local);
                    }
                }
            }

            return data;
        }

        private static void AddVisibleFaces(
            ChunkMeshData data,
            Chunk chunk,
            IVoxelLookup world,
            Voxel voxel,
            int3 local,
            int3 worldPos)
        {
            Color32 color = VoxelPalette.GetColor(voxel.Type, worldPos);

            for (int face = 0; face < 6; face++)
            {
                int3 neighbor = local + FaceDirections[face];

                // Voisin dans ce chunk : accès direct ; sinon on interroge le monde.
                Voxel neighborVoxel = chunk.Contains(neighbor.x, neighbor.y, neighbor.z)
                    ? chunk.GetVoxel(neighbor.x, neighbor.y, neighbor.z)
                    : world.GetVoxel(worldPos + FaceDirections[face]);

                // Une face n'est visible que contre de l'air : les faces entre
                // deux voxels pleins (ou entre deux eaux) sont supprimées.
                if (neighborVoxel.IsAir)
                {
                    AddFace(data, worldPos - chunk.WorldOrigin, face, color);
                }
            }
        }

        private static void AddFace(ChunkMeshData data, int3 localPos, int face, Color32 color)
        {
            int baseIndex = data.Vertices.Count;
            var normal = (Vector3)(float3)FaceDirections[face];

            for (int i = 0; i < 4; i++)
            {
                float3 corner = (float3)localPos + Corners[FaceCorners[face][i]];
                data.Vertices.Add(corner);
                data.Normals.Add(normal);
                data.Colors.Add(color);
            }

            // Deux triangles : (0,1,2) et (2,1,3) du quad.
            data.Triangles.Add(baseIndex + 0);
            data.Triangles.Add(baseIndex + 1);
            data.Triangles.Add(baseIndex + 2);
            data.Triangles.Add(baseIndex + 2);
            data.Triangles.Add(baseIndex + 1);
            data.Triangles.Add(baseIndex + 3);
        }
    }
}
