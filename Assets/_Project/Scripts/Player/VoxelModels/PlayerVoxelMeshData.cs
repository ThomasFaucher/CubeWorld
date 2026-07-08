using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWorld.Player.VoxelModels
{
    internal sealed class PlayerVoxelMeshData
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Color32> Colors = new();
        public readonly List<int> Triangles = new();

        public Mesh ToMesh(string meshName)
        {
            var mesh = new Mesh { name = meshName };

            if (Vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetColors(Colors);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
