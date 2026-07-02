using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWorld.World
{
    /// <summary>
    /// Données de mesh d'un chunk, en mémoire native : remplies par
    /// <see cref="ChunkMeshBuildJob"/> puis converties en Mesh Unity sur le
    /// thread principal. Doit être libérée explicitement (Dispose) une fois le
    /// Mesh créé, ou si le job est abandonné (chunk déchargé entre-temps).
    /// </summary>
    public sealed class ChunkMeshData : IDisposable
    {
        public NativeList<float3> Vertices;
        public NativeList<float3> Normals;
        public NativeList<Color32> Colors;
        public NativeList<int> Triangles;

        public ChunkMeshData(Allocator allocator)
        {
            Vertices = new NativeList<float3>(4096, allocator);
            Normals = new NativeList<float3>(4096, allocator);
            Colors = new NativeList<Color32>(4096, allocator);
            Triangles = new NativeList<int>(6144, allocator);
        }

        public bool IsEmpty => Vertices.Length == 0;

        public Mesh ToMesh()
        {
            var mesh = new Mesh
            {
                // Un chunk 32³ très découpé peut dépasser 65 535 sommets.
                indexFormat = IndexFormat.UInt32,
            };

            mesh.SetVertices(Vertices.AsArray());
            mesh.SetNormals(Normals.AsArray());
            mesh.SetColors(Colors.AsArray());
            mesh.SetIndices(Triangles.AsArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        public void Dispose()
        {
            if (Vertices.IsCreated)
            {
                Vertices.Dispose();
            }

            if (Normals.IsCreated)
            {
                Normals.Dispose();
            }

            if (Colors.IsCreated)
            {
                Colors.Dispose();
            }

            if (Triangles.IsCreated)
            {
                Triangles.Dispose();
            }
        }
    }
}
