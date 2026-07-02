using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Job Burst qui transforme les voxels d'un chunk en mesh : pour chaque voxel
    /// solide, seules les faces exposées à l'air sont générées (face culling).
    /// Chaque face a ses 4 sommets propres avec une normale de face — c'est ce
    /// qui donne le rendu flat shading. La couleur du voxel est écrite dans les
    /// couleurs de vertex. Les 6 voisins directs sont fournis à part : un voisin
    /// non chargé (tableau non créé) est traité comme de l'air.
    /// Les faces d'eau sont émises dans un second jeu de buffers (mesh à part,
    /// rendu avec un matériau transparent) plutôt que dans le mesh opaque.
    /// </summary>
    [BurstCompile]
    internal struct ChunkMeshBuildJob : IJob
    {
        [ReadOnly] public NativeArray<Voxel> Voxels;
        [ReadOnly] public NativeArray<Voxel> NeighborBack;   // z - 1
        [ReadOnly] public NativeArray<Voxel> NeighborFront;  // z + 1
        [ReadOnly] public NativeArray<Voxel> NeighborTop;    // y + 1
        [ReadOnly] public NativeArray<Voxel> NeighborBottom; // y - 1
        [ReadOnly] public NativeArray<Voxel> NeighborLeft;   // x - 1
        [ReadOnly] public NativeArray<Voxel> NeighborRight;  // x + 1

        public int Size;
        public int3 Origin;

        public NativeList<float3> OpaqueVertices;
        public NativeList<float3> OpaqueNormals;
        public NativeList<Color32> OpaqueColors;
        public NativeList<int> OpaqueTriangles;

        public NativeList<float3> WaterVertices;
        public NativeList<float3> WaterNormals;
        public NativeList<Color32> WaterColors;
        public NativeList<int> WaterTriangles;

        public void Execute()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Voxel voxel = Voxels[ToIndex(x, y, z)];
                        if (voxel.IsAir)
                        {
                            continue;
                        }

                        AddVisibleFaces(voxel, new int3(x, y, z));
                    }
                }
            }
        }

        private void AddVisibleFaces(Voxel voxel, int3 local)
        {
            Color32 color = VoxelPalette.GetColor(voxel.Type, Origin + local);
            bool isWater = voxel.Type == VoxelType.Water;

            for (int face = 0; face < 6; face++)
            {
                int3 neighborLocal = local + GetFaceDirection(face);

                // Une face n'est visible que contre de l'air : les faces entre
                // deux voxels pleins (ou entre deux eaux) sont supprimées.
                if (SampleVoxel(neighborLocal).IsAir)
                {
                    AddFace(local, face, color, isWater);
                }
            }
        }

        // Voisin dans ce chunk : accès direct ; sinon on lit le tableau du
        // voisin correspondant (Air si ce voisin n'est pas chargé).
        private Voxel SampleVoxel(int3 local)
        {
            if (local.x >= 0 && local.x < Size && local.y >= 0 && local.y < Size && local.z >= 0 && local.z < Size)
            {
                return Voxels[ToIndex(local.x, local.y, local.z)];
            }

            if (local.x < 0)
            {
                return SampleNeighbor(NeighborLeft, Size - 1, local.y, local.z);
            }

            if (local.x >= Size)
            {
                return SampleNeighbor(NeighborRight, 0, local.y, local.z);
            }

            if (local.y < 0)
            {
                return SampleNeighbor(NeighborBottom, local.x, Size - 1, local.z);
            }

            if (local.y >= Size)
            {
                return SampleNeighbor(NeighborTop, local.x, 0, local.z);
            }

            return local.z < 0
                ? SampleNeighbor(NeighborBack, local.x, local.y, Size - 1)
                : SampleNeighbor(NeighborFront, local.x, local.y, 0);
        }

        private Voxel SampleNeighbor(NativeArray<Voxel> neighbor, int x, int y, int z)
        {
            return neighbor.IsCreated ? neighbor[ToIndex(x, y, z)] : Voxel.Air;
        }

        private void AddFace(int3 localPos, int face, Color32 color, bool isWater)
        {
            NativeList<float3> vertices = isWater ? WaterVertices : OpaqueVertices;
            NativeList<float3> normals = isWater ? WaterNormals : OpaqueNormals;
            NativeList<Color32> colors = isWater ? WaterColors : OpaqueColors;
            NativeList<int> triangles = isWater ? WaterTriangles : OpaqueTriangles;

            int baseIndex = vertices.Length;
            float3 normal = GetFaceDirection(face);

            for (int i = 0; i < 4; i++)
            {
                float3 corner = (float3)localPos + GetCorner(GetFaceCornerIndex(face, i));
                vertices.Add(corner);
                normals.Add(normal);
                colors.Add(color);
            }

            // Deux triangles : (0,1,2) et (2,1,3) du quad.
            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 3);
        }

        private int ToIndex(int x, int y, int z)
        {
            return x + Size * (y + Size * z);
        }

        // Les 8 coins d'un voxel unitaire, relatifs à son coin (0,0,0).
        // (Tables exprimées en switch plutôt qu'en tableaux : les jobs Burst ne
        // peuvent pas référencer de tableaux managés.)
        private static float3 GetCorner(int index) => index switch
        {
            0 => new float3(0f, 0f, 0f),
            1 => new float3(1f, 0f, 0f),
            2 => new float3(1f, 1f, 0f),
            3 => new float3(0f, 1f, 0f),
            4 => new float3(0f, 0f, 1f),
            5 => new float3(1f, 0f, 1f),
            6 => new float3(1f, 1f, 1f),
            _ => new float3(0f, 1f, 1f), // 7
        };

        // Direction du voisin à tester pour chaque face.
        private static int3 GetFaceDirection(int face) => face switch
        {
            0 => new int3(0, 0, -1), // arrière
            1 => new int3(0, 0, 1),  // avant
            2 => new int3(0, 1, 0),  // dessus
            3 => new int3(0, -1, 0), // dessous
            4 => new int3(-1, 0, 0), // gauche
            _ => new int3(1, 0, 0),  // droite
        };

        // Les 4 coins de chaque face, ordonnés pour un enroulement horaire
        // (faces visibles de l'extérieur avec le winding Unity).
        private static int GetFaceCornerIndex(int face, int i) => face switch
        {
            0 => i switch { 0 => 0, 1 => 3, 2 => 1, _ => 2 }, // arrière
            1 => i switch { 0 => 5, 1 => 6, 2 => 4, _ => 7 }, // avant
            2 => i switch { 0 => 3, 1 => 7, 2 => 2, _ => 6 }, // dessus
            3 => i switch { 0 => 1, 1 => 5, 2 => 0, _ => 4 }, // dessous
            4 => i switch { 0 => 4, 1 => 7, 2 => 0, _ => 3 }, // gauche
            _ => i switch { 0 => 1, 1 => 2, 2 => 5, _ => 6 }, // droite
        };
    }
}
