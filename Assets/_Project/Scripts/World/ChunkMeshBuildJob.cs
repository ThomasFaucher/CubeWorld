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

        // Touffes d'herbe : brins en quads croisés au-dessus des voxels Grass
        // exposés. Mesh à part (pas de collider — voir WorldBootstrap).
        public NativeList<float3> FoliageVertices;
        public NativeList<float3> FoliageNormals;
        public NativeList<Color32> FoliageColors;
        public NativeList<int> FoliageTriangles;

        public float GrassTuftDensity;
        public float GrassTuftMinSize;
        public float GrassTuftMaxSize;
        public float GrassTuftMinWidth;
        public float GrassTuftMaxWidth;

        public float FlowerDensity;
        public float FlowerMinSize;
        public float FlowerMaxSize;
        public float FlowerMinWidth;
        public float FlowerMaxWidth;

        // Paramètres biome (même graine que TerrainGenerator) pour teintes vertex.
        public float2 TemperatureSeedOffset;
        public float2 HumiditySeedOffset;
        public float BiomeNoiseScale;
        public int BiomeOctaves;
        public float SnowTemperatureThreshold;
        public float DesertTemperatureThreshold;
        public float DesertHumidityThreshold;
        public float SwampHumidityThreshold;
        public float ForestHumidityThreshold;

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
            int3 worldPos = Origin + local;
            BiomeType biome = SampleBiome(worldPos.x, worldPos.z);
            Color32 color = VoxelPalette.GetColor(voxel.Type, worldPos, biome);
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
            // libre — VoxelType.Grass n'existe déjà que sur Forêt/Plaines/Marais.
            if (topExposed && voxel.Type == VoxelType.Grass)
            {
                TryAddGrassTuft(local, biome);
                if (biome == BiomeType.Plains)
                {
                    TryAddFlower(local);
                }
            }
        }

        private BiomeType SampleBiome(int worldX, int worldZ)
        {
            return BiomeShape.Sample(
                worldX,
                worldZ,
                TemperatureSeedOffset,
                HumiditySeedOffset,
                BiomeNoiseScale,
                BiomeOctaves,
                SnowTemperatureThreshold,
                DesertTemperatureThreshold,
                DesertHumidityThreshold,
                SwampHumidityThreshold,
                ForestHumidityThreshold);
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

        // Tirage déterministe : un voxel Grass sur GrassTuftDensity porte 4 à 7
        // brins en quads croisés (fins), hauteur/largeur/position/teinte hashées.
        private void TryAddGrassTuft(int3 local, BiomeType biome)
        {
            int3 worldPos = Origin + local;

            if (HashToUnit(worldPos, 0) >= GrassTuftDensity)
            {
                return;
            }

            int tuftCount = 4 + (int)(HashToUnit(worldPos, 1) * 4f);
            for (int i = 0; i < tuftCount; i++)
            {
                AddGrassBlade(local, worldPos, i, biome);
            }
        }

        // Fleurs Plains : 1–3 brins colorés (même géométrie croisée que l'herbe).
        private void TryAddFlower(int3 local)
        {
            int3 worldPos = Origin + local;

            if (HashToUnit(worldPos, 40) >= FlowerDensity)
            {
                return;
            }

            int flowerCount = 1 + (int)(HashToUnit(worldPos, 41) * 3f);
            for (int i = 0; i < flowerCount; i++)
            {
                AddFoliageBlade(
                    local,
                    worldPos,
                    i,
                    FlowerMinSize,
                    FlowerMaxSize,
                    FlowerMinWidth,
                    FlowerMaxWidth,
                    50,
                    FlowerColor(worldPos, i));
            }
        }

        // Un brin = deux quads croisés (plans X et Z), légers et lisibles de
        // tous les angles — pas une boîte épaisse qui ressemble à un débris.
        private void AddGrassBlade(int3 local, int3 worldPos, int index, BiomeType biome)
        {
            AddFoliageBlade(
                local,
                worldPos,
                index,
                GrassTuftMinSize,
                GrassTuftMaxSize,
                GrassTuftMinWidth,
                GrassTuftMaxWidth,
                10,
                GrassTuftColor(worldPos, index, biome));
        }

        private void AddFoliageBlade(
            int3 local,
            int3 worldPos,
            int index,
            float minSize,
            float maxSize,
            float minWidth,
            float maxWidth,
            int saltBase,
            Color32 color)
        {
            int salt = index * 4;
            float height = math.lerp(minSize, maxSize, HashToUnit(worldPos, saltBase + salt));
            float width = math.lerp(minWidth, maxWidth, HashToUnit(worldPos, saltBase + 3 + salt));

            float maxOffset = (1f - width) * 0.5f;
            float offsetX = ((HashToUnit(worldPos, saltBase + 1 + salt) * 2f) - 1f) * maxOffset;
            float offsetZ = ((HashToUnit(worldPos, saltBase + 2 + salt) * 2f) - 1f) * maxOffset;

            float yaw = HashToUnit(worldPos, saltBase + 4 + salt) * math.PI;
            float cosYaw = math.cos(yaw);
            float sinYaw = math.sin(yaw);

            var baseCenter = (float3)local + new float3(0.5f + offsetX, 1f, 0.5f + offsetZ);
            float halfW = width * 0.5f;

            AddFoliageQuadDoubleSided(
                baseCenter + RotateYaw(new float3(-halfW, 0f, 0f), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(halfW, 0f, 0f), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(-halfW, height, 0f), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(halfW, height, 0f), cosYaw, sinYaw),
                math.normalize(RotateYaw(new float3(0f, 0f, 1f), cosYaw, sinYaw)),
                color);

            AddFoliageQuadDoubleSided(
                baseCenter + RotateYaw(new float3(0f, 0f, -halfW), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(0f, 0f, halfW), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(0f, height, -halfW), cosYaw, sinYaw),
                baseCenter + RotateYaw(new float3(0f, height, halfW), cosYaw, sinYaw),
                math.normalize(RotateYaw(new float3(1f, 0f, 0f), cosYaw, sinYaw)),
                color);
        }

        private static float3 RotateYaw(float3 local, float cosYaw, float sinYaw)
        {
            return new float3(
                (local.x * cosYaw) - (local.z * sinYaw),
                local.y,
                (local.x * sinYaw) + (local.z * cosYaw));
        }

        // Émet un quad recto + verso (Cull Back du shader terrain).
        // Coins : bl, br, tl, tr — winding horaire vu depuis la normale.
        private void AddFoliageQuadDoubleSided(
            float3 bottomLeft,
            float3 bottomRight,
            float3 topLeft,
            float3 topRight,
            float3 normal,
            Color32 color)
        {
            AddFoliageQuad(bottomLeft, bottomRight, topLeft, topRight, normal, color);
            AddFoliageQuad(bottomRight, bottomLeft, topRight, topLeft, -normal, color);
        }

        private void AddFoliageQuad(
            float3 bottomLeft,
            float3 bottomRight,
            float3 topLeft,
            float3 topRight,
            float3 normal,
            Color32 color)
        {
            int baseIndex = FoliageVertices.Length;

            FoliageVertices.Add(bottomLeft);
            FoliageVertices.Add(topLeft);
            FoliageVertices.Add(bottomRight);
            FoliageVertices.Add(topRight);

            FoliageNormals.Add(normal);
            FoliageNormals.Add(normal);
            FoliageNormals.Add(normal);
            FoliageNormals.Add(normal);

            FoliageColors.Add(color);
            FoliageColors.Add(color);
            FoliageColors.Add(color);
            FoliageColors.Add(color);

            FoliageTriangles.Add(baseIndex + 0);
            FoliageTriangles.Add(baseIndex + 1);
            FoliageTriangles.Add(baseIndex + 2);
            FoliageTriangles.Add(baseIndex + 2);
            FoliageTriangles.Add(baseIndex + 1);
            FoliageTriangles.Add(baseIndex + 3);
        }

        // Vert un peu plus soutenu que le sol, teinté Plains / Forest / Swamp.
        private static Color32 GrassTuftColor(int3 worldPos, int index, BiomeType biome)
        {
            Color32 baseColor = biome switch
            {
                BiomeType.Forest => new Color32(42, 148, 48, 255),
                BiomeType.Swamp => new Color32(28, 62, 30, 255),
                _ => new Color32(78, 188, 52, 255),
            };

            float factor = 1f + (((HashToUnit(worldPos, 20 + index) * 2f) - 1f) * 0.06f);
            return ScaleColor(baseColor, factor);
        }

        private static Color32 FlowerColor(int3 worldPos, int index)
        {
            int paletteIndex = (int)(HashToUnit(worldPos, 70 + index) * 4f);
            Color32 baseColor = paletteIndex switch
            {
                0 => new Color32(245, 210, 55, 255),
                1 => new Color32(250, 250, 250, 255),
                2 => new Color32(235, 120, 160, 255),
                _ => new Color32(255, 170, 70, 255),
            };
            float factor = 1f + (((HashToUnit(worldPos, 80 + index) * 2f) - 1f) * 0.08f);
            return ScaleColor(baseColor, factor);
        }

        private static Color32 ScaleColor(Color32 baseColor, float factor)
        {
            return new Color32(
                (byte)math.clamp((int)(baseColor.r * factor), 0, 255),
                (byte)math.clamp((int)(baseColor.g * factor), 0, 255),
                (byte)math.clamp((int)(baseColor.b * factor), 0, 255),
                255
            );
        }

        // Hash déterministe [0, 1) dérivé de la position monde + d'un sel.
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

        private static int3 GetFaceDirection(int face) => face switch
        {
            0 => new int3(0, 0, -1), // arrière
            1 => new int3(0, 0, 1),  // avant
            2 => new int3(0, 1, 0),  // dessus
            3 => new int3(0, -1, 0), // dessous
            4 => new int3(-1, 0, 0), // gauche
            _ => new int3(1, 0, 0),  // droite
        };

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
