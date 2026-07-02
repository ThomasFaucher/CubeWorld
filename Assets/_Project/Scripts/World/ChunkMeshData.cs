using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWorld.World
{
    /// <summary>
    /// Données de mesh d'un chunk, accumulées par le builder puis converties
    /// en Mesh Unity. Sépare le calcul (pur C#) de l'objet moteur.
    /// </summary>
    public sealed class ChunkMeshData
    {
        public List<Vector3> Vertices { get; } = new(4096);
        public List<Vector3> Normals { get; } = new(4096);
        public List<Color32> Colors { get; } = new(4096);
        public List<int> Triangles { get; } = new(6144);

        public bool IsEmpty => Vertices.Count == 0;

        public Mesh ToMesh()
        {
            var mesh = new Mesh
            {
                // Un chunk 32³ très découpé peut dépasser 65 535 sommets.
                indexFormat = IndexFormat.UInt32,
            };

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetColors(Colors);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
