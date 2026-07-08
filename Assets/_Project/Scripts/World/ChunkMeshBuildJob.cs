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
    /// non chargé (tableau vide, longueur 0) est traité comme de l'air.
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

        // Touffes d'herbe : petits cubes posés au-dessus des voxels Grass dont la
        // face du dessus est exposée à l'air. Mesh à part (pas de collider dessus,
        // voir WorldBootstrap) : seul le meshing sait quelles faces sont exposées,
        // donc c'est ici (et pas à la génération) que l'éligibilité se décide.
        public NativeList<float3> FoliageVertices;
        public NativeList<float3> FoliageNormals;
        public NativeList<Color32> FoliageColors;
        public NativeList<int> FoliageTriangles;

        public float GrassTuftDensity;
        public float GrassTuftMinSize;
        public float GrassTuftMaxSize;

        // Index de la face "dessus" dans GetFaceDirection/GetFaceCornerIndex.
        private const int TopFaceIndex = 2;

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
            bool topExposed = false;

            for (int face = 0; face < 6; face++)
            {
                int3 neighborLocal = local + GetFaceDirection(face);

                if (IsFaceVisible(isWater, SampleVoxel(neighborLocal)))
                {
                    AddFace(local, face, color, isWater);
                    topExposed |= face == TopFaceIndex;
                }
            }

            // L'herbe ne pousse que là où un voxel Grass est réellement à l'air
            // libre — un Grass enterré (rare, mais possible aux coutures) n'en
            // porte pas. VoxelType.Grass n'existe déjà que sur les biomes
            // Forêt/Plaines (voir TerrainShape.CreateVoxel), donc aucune
            // vérification de biome n'est nécessaire ici.
            if (topExposed && voxel.Type == VoxelType.Grass)
            {
                TryAddGrassTuft(local);
            }
        }

        // Une face solide est visible contre de l'air ou contre de l'eau (elle
        // doit se voir *à travers* l'eau translucide, ex. le fond d'un lac).
        // Une face d'eau n'est visible que contre de l'air (sa surface) : entre
        // deux voxels d'eau ou contre du solide, elle serait redondante avec la
        // face déjà dessinée par ce voisin (ou invisible de toute façon).
        private static bool IsFaceVisible(bool currentIsWater, Voxel neighbor)
        {
            if (neighbor.IsAir)
            {
                return true;
            }

            return !currentIsWater && neighbor.Type == VoxelType.Water;
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

        // Un job Burst ne peut pas recevoir un NativeArray par défaut (non
        // construit) : un voisin non chargé est représenté par un tableau
        // valide mais de longueur 0, reconnu ici plutôt que via IsCreated.
        private Voxel SampleNeighbor(NativeArray<Voxel> neighbor, int x, int y, int z)
        {
            return neighbor.Length > 0 ? neighbor[ToIndex(x, y, z)] : Voxel.Air;
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

        // Tirage déterministe (même monde => mêmes touffes) : un voxel Grass sur
        // GrassTuftDensity porte 3 à 5 brins fins, hauteur/position/teinte
        // dérivées du même hash avec des sels différents.
        private void TryAddGrassTuft(int3 local)
        {
            int3 worldPos = Origin + local;

            if (HashToUnit(worldPos, 0) >= GrassTuftDensity)
            {
                return;
            }

            int tuftCount = 3 + (int)(HashToUnit(worldPos, 1) * 3f);
            for (int i = 0; i < tuftCount; i++)
            {
                AddGrassBlade(local, worldPos, i);
            }
        }

        // Un brin = une boîte fine et haute (pas un cube : des cubes flottants
        // ressemblent à des débris, des brins élancés ressemblent à de l'herbe),
        // posée sur la face supérieure du voxel porteur.
        private void AddGrassBlade(int3 local, int3 worldPos, int index)
        {
            int salt = index * 4;
            float height = math.lerp(GrassTuftMinSize, GrassTuftMaxSize, HashToUnit(worldPos, 10 + salt));
            float width = 0.10f + (HashToUnit(worldPos, 13 + salt) * 0.08f);

            // Décalage horizontal borné pour que le brin reste dans l'empreinte
            // du voxel porteur (évite qu'il déborde visiblement sur un voxel
            // voisin qui pourrait être de l'air).
            float maxOffset = (1f - width) * 0.5f;
            float offsetX = ((HashToUnit(worldPos, 11 + salt) * 2f) - 1f) * maxOffset;
            float offsetZ = ((HashToUnit(worldPos, 12 + salt) * 2f) - 1f) * maxOffset;

            var center = (float3)local + new float3(0.5f + offsetX, 1f + (height * 0.5f), 0.5f + offsetZ);
            Color32 color = GrassTuftColor(worldPos, index);

            AddFoliageBox(center, new float3(width * 0.5f, height * 0.5f, width * 0.5f), color);
        }

        // Boîte autonome dans le mesh Foliage, en réutilisant les mêmes tables
        // de coins/faces que AddFace. La face du dessous est omise : posée sur
        // le sol, elle n'est jamais visible (5 faces au lieu de 6 par brin).
        private void AddFoliageBox(float3 centerLocal, float3 halfExtents, Color32 color)
        {
            for (int face = 0; face < 6; face++)
            {
                if (face == 3)
                {
                    continue; // dessous
                }

                int baseIndex = FoliageVertices.Length;
                float3 normal = GetFaceDirection(face);

                for (int i = 0; i < 4; i++)
                {
                    float3 corner = ((GetCorner(GetFaceCornerIndex(face, i)) - 0.5f) * (halfExtents * 2f)) + centerLocal;
                    FoliageVertices.Add(corner);
                    FoliageNormals.Add(normal);
                    FoliageColors.Add(color);
                }

                FoliageTriangles.Add(baseIndex + 0);
                FoliageTriangles.Add(baseIndex + 1);
                FoliageTriangles.Add(baseIndex + 2);
                FoliageTriangles.Add(baseIndex + 2);
                FoliageTriangles.Add(baseIndex + 1);
                FoliageTriangles.Add(baseIndex + 3);
            }
        }

        // Vert un peu plus soutenu que le sol (voir VoxelPalette.Grass) pour que
        // les brins se détachent visuellement, avec une variation par brin plus
        // marquée que celle du terrain (une touffe vivante n'est pas uniforme).
        private static Color32 GrassTuftColor(int3 worldPos, int index)
        {
            var baseColor = new Color32(58, 178, 50, 255);
            float factor = 1f + (((HashToUnit(worldPos, 20 + index) * 2f) - 1f) * 0.15f);

            return new Color32(
                (byte)math.clamp((int)(baseColor.r * factor), 0, 255),
                (byte)math.clamp((int)(baseColor.g * factor), 0, 255),
                (byte)math.clamp((int)(baseColor.b * factor), 0, 255),
                255
            );
        }

        // Hash déterministe [0, 1) dérivé de la position monde + d'un sel (pour
        // tirer plusieurs valeurs indépendantes à la même position sans les
        // corréler) — même principe que VoxelPalette.Vary, gardé local au job.
        private static float HashToUnit(int3 position, int salt)
        {
            uint hash = math.hash(new int4(position, salt));
            return (hash & 0x00FFFFFFu) / (float)0x01000000u;
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
