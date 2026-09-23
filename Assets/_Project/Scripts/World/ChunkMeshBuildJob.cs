using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Job Burst qui transforme les voxels d'un chunk en mesh : les faces
    /// exposées à l'air (ou à l'eau) sont fusionnées en rectangles par un
    /// greedy meshing 2D (balayage par plan, par axe), plutôt qu'un quad par
    /// voxel — réduit drastiquement le nombre de sommets/triangles sur les
    /// grandes surfaces planes (sol, murs de falaise...). Chaque quad fusionné
    /// a ses 4 sommets propres avec une normale de face — c'est ce qui donne
    /// le rendu flat shading. La couleur est échantillonnée au centre du
    /// rectangle fusionné (teinte régionale continue, voir VoxelPalette) et
    /// écrite dans les couleurs de vertex. Les 6 voisins directs sont fournis
    /// à part : un voisin non chargé (tableau vide, longueur 0) est traité
    /// comme de l'air. Les faces d'eau sont émises dans un second jeu de
    /// buffers (mesh à part, rendu avec un matériau transparent) plutôt que
    /// dans le mesh opaque.
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

        // LOD : les chunks lointains n'ont pas besoin de touffes d'herbe (voir
        // WorldConfig.LodNearDistance) — le terrain lui-même (greedy mesh)
        // garde toujours le détail complet, seul cet extra coûteux est coupé.
        public bool IncludeFoliage;

        public void Execute()
        {
            BuildGreedyMesh();

            if (IncludeFoliage)
            {
                PlantFoliage();
            }
        }

        // Un rectangle fusionné par cellule de masque : Type=0 (Air) signifie
        // "pas de face ici". Sign=+1 : la face appartient au voxel "arrière"
        // (coordonnée w-1 le long de l'axe) et pointe vers +axe. Sign=-1 :
        // elle appartient au voxel "avant" (coordonnée w) et pointe vers -axe.
        // Struct blittable (byte/sbyte) : compatible NativeArray en job Burst.
        private struct FaceMaskCell
        {
            public byte Type;
            public sbyte Sign;

            // Occlusion ambiante des 4 coins de la face, 2 bits par coin
            // (niveau 0 = dégagé … 3 = coin enfermé), index de coin = du + 2*dv
            // dans le repère (u, v) du plan. Fait partie de la clé de fusion :
            // deux faces ne fusionnent que si leur AO est identique, ce qui
            // garde les grands aplats dégagés fusionnés (AO = 0 partout) et
            // découpe seulement au pied des murs / dans les creux.
            public byte Ao;
        }

        // Niveau d'occlusion (0..3) -> alpha de vertex, même convention que
        // CharacterModel.VoxelGridMesher (lu par les shaders VoxelTerrain /
        // VoxelCharacter : alpha 255 = aucune occlusion).
        private static byte AoLevelToAlpha(int level)
        {
            switch (level)
            {
                case 0: return 255;
                case 1: return 217;
                case 2: return 179;
                default: return 128;
            }
        }

        // Greedy meshing par balayage de plans : pour chaque axe (X, Y, Z), on
        // parcourt les Size+1 plans perpendiculaires (un de plus que le nombre
        // de voxels : chaque plan est la frontière entre le voxel w-1 et le
        // voxel w, les deux plans extrêmes touchant les chunks voisins). Sur
        // chaque plan, un masque 2D indique quelle face (type + orientation)
        // s'y trouve, puis un algorithme de fusion de rectangles (comme pour
        // Minecraft/0fps) regroupe les cellules identiques adjacentes en un
        // minimum de quads.
        private void BuildGreedyMesh()
        {
            MeshAxis(0);
            MeshAxis(1);
            MeshAxis(2);
        }

        private void MeshAxis(int axis)
        {
            GetPerpendicularAxes(axis, out int uAxis, out int vAxis);

            var mask = new NativeArray<FaceMaskCell>(Size * Size, Allocator.Temp);
            var visited = new NativeArray<bool>(Size * Size, Allocator.Temp);

            for (int w = 0; w <= Size; w++)
            {
                BuildMask(axis, uAxis, vAxis, w, mask);
                MergeAndEmit(axis, uAxis, vAxis, w, mask, visited);
            }

            mask.Dispose();
            visited.Dispose();
        }

        // Correspond à la convention géométrique de l'ancien meshing par
        // voxel (voir GetFaceCornerIndex historique) : X <-> (Z, Y), Y <-> (X, Z),
        // Z <-> (X, Y). Ne pas permuter sans revalider le winding des faces.
        private static void GetPerpendicularAxes(int axis, out int uAxis, out int vAxis)
        {
            switch (axis)
            {
                case 0: // X : gauche/droite
                    uAxis = 2;
                    vAxis = 1;
                    break;
                case 1: // Y : dessus/dessous
                    uAxis = 0;
                    vAxis = 2;
                    break;
                default: // Z : arrière/avant
                    uAxis = 0;
                    vAxis = 1;
                    break;
            }
        }

        private void BuildMask(int axis, int uAxis, int vAxis, int w, NativeArray<FaceMaskCell> mask)
        {
            for (int v = 0; v < Size; v++)
            {
                for (int u = 0; u < Size; u++)
                {
                    int3 posBack = MakeCoord(axis, w - 1, uAxis, u, vAxis, v);
                    int3 posFront = MakeCoord(axis, w, uAxis, u, vAxis, v);
                    FaceMaskCell cell = ComputeFaceCell(SampleVoxel(posBack), SampleVoxel(posFront));

                    // Pas d'AO sur l'eau (son alpha porte la transparence).
                    if (cell.Type != 0 && cell.Type != (byte)VoxelType.Water)
                    {
                        // Couche "devant" la face (côté air) : c'est là que se
                        // trouvent les voxels qui occultent ses coins.
                        int airLayer = cell.Sign > 0 ? w : w - 1;
                        cell.Ao = ComputeFaceAo(axis, uAxis, vAxis, airLayer, u, v);
                    }

                    mask[u + (v * Size)] = cell;
                }
            }
        }

        // AO voxel classique (0fps) : pour chaque coin, on regarde dans la
        // couche devant la face les deux voisins de côté et le voisin
        // diagonal. Deux côtés pleins = coin enfermé (niveau max).
        private byte ComputeFaceAo(int axis, int uAxis, int vAxis, int layer, int u, int v)
        {
            int packed = 0;
            for (int dv = 0; dv < 2; dv++)
            {
                int sv = dv == 1 ? 1 : -1;
                for (int du = 0; du < 2; du++)
                {
                    int su = du == 1 ? 1 : -1;

                    bool side1 = IsOccluder(MakeCoord(axis, layer, uAxis, u + su, vAxis, v));
                    bool side2 = IsOccluder(MakeCoord(axis, layer, uAxis, u, vAxis, v + sv));
                    bool diagonal = IsOccluder(MakeCoord(axis, layer, uAxis, u + su, vAxis, v + sv));

                    int level = side1 && side2 ? 3 : (side1 ? 1 : 0) + (side2 ? 1 : 0) + (diagonal ? 1 : 0);
                    packed |= level << ((du + (2 * dv)) * 2);
                }
            }

            return (byte)packed;
        }

        // Voxel plein qui occulte (ni air, ni eau). Les voisins en diagonale
        // de chunk (hors limites sur 2 axes ou plus) ne sont pas fournis au
        // job : traités comme dégagés — SampleVoxel ne sait lire qu'un seul
        // voisin direct à la fois.
        private bool IsOccluder(int3 local)
        {
            int outside = (local.x < 0 || local.x >= Size ? 1 : 0)
                + (local.y < 0 || local.y >= Size ? 1 : 0)
                + (local.z < 0 || local.z >= Size ? 1 : 0);
            if (outside > 1)
            {
                return false;
            }

            Voxel voxel = SampleVoxel(local);
            return !voxel.IsAir && voxel.Type != VoxelType.Water;
        }

        // Reproduit exactement les règles de IsFaceVisible appliquées de
        // chaque côté de la frontière : au plus une face possible par
        // frontière (jamais les deux à la fois — voir la doc de la classe).
        private static FaceMaskCell ComputeFaceCell(Voxel back, Voxel front)
        {
            if (!back.IsAir && IsFaceVisible(back.Type == VoxelType.Water, front))
            {
                return new FaceMaskCell { Type = (byte)back.Type, Sign = 1 };
            }

            if (!front.IsAir && IsFaceVisible(front.Type == VoxelType.Water, back))
            {
                return new FaceMaskCell { Type = (byte)front.Type, Sign = -1 };
            }

            return default;
        }

        // Fusionne les cellules identiques adjacentes du masque en rectangles
        // maximaux (algorithme glouton standard : extension en largeur, puis
        // en hauteur tant que la ligne entière correspond), puis émet un quad
        // par rectangle.
        private void MergeAndEmit(int axis, int uAxis, int vAxis, int w, NativeArray<FaceMaskCell> mask, NativeArray<bool> visited)
        {
            for (int i = 0; i < visited.Length; i++)
            {
                visited[i] = false;
            }

            for (int v = 0; v < Size; v++)
            {
                for (int u = 0; u < Size;)
                {
                    int idx = u + (v * Size);
                    FaceMaskCell cell = mask[idx];

                    if (visited[idx] || cell.Type == 0)
                    {
                        u++;
                        continue;
                    }

                    int width = 1;
                    while (u + width < Size && MatchesCell(mask, visited, u + width, v, Size, cell))
                    {
                        width++;
                    }

                    int height = 1;
                    while (v + height < Size && RowMatchesCell(mask, visited, u, v + height, width, Size, cell))
                    {
                        height++;
                    }

                    for (int hh = 0; hh < height; hh++)
                    {
                        for (int ww = 0; ww < width; ww++)
                        {
                            visited[(u + ww) + ((v + hh) * Size)] = true;
                        }
                    }

                    EmitQuad(axis, uAxis, vAxis, w, u, u + width, v, v + height, (VoxelType)cell.Type, cell.Sign, cell.Ao);
                    u += width;
                }
            }
        }

        private static bool MatchesCell(NativeArray<FaceMaskCell> mask, NativeArray<bool> visited, int u, int v, int size, FaceMaskCell cell)
        {
            int idx = u + (v * size);
            FaceMaskCell other = mask[idx];
            return !visited[idx] && other.Type == cell.Type && other.Sign == cell.Sign && other.Ao == cell.Ao;
        }

        private static bool RowMatchesCell(NativeArray<FaceMaskCell> mask, NativeArray<bool> visited, int u, int v, int width, int size, FaceMaskCell cell)
        {
            for (int k = 0; k < width; k++)
            {
                if (!MatchesCell(mask, visited, u + k, v, size, cell))
                {
                    return false;
                }
            }

            return true;
        }

        // Émet le quad fusionné [u0,u1) x [v0,v1) sur le plan w de cet axe.
        // La couleur est échantillonnée une seule fois au centre du rectangle
        // (teinte régionale continue et de faible amplitude — voir
        // VoxelPalette — un dégradé par sommet serait imperceptible mais
        // coûterait un échantillonnage par coin).
        private void EmitQuad(int axis, int uAxis, int vAxis, int w, int u0, int u1, int v0, int v1, VoxelType type, sbyte sign, byte ao)
        {
            bool isWater = type == VoxelType.Water;

            int wVoxel = sign > 0 ? w - 1 : w;
            int uCenter = (u0 + u1 - 1) / 2;
            int vCenter = (v0 + v1 - 1) / 2;
            int3 worldPos = Origin + MakeCoord(axis, wVoxel, uAxis, uCenter, vAxis, vCenter);
            BiomeType biome = SampleBiome(worldPos.x, worldPos.z);
            Color32 color = VoxelPalette.GetColor(type, worldPos, biome);

            var normal = default(float3);
            SetComponentF(ref normal, axis, sign);

            float3 c00 = MakeCornerF(axis, w, uAxis, u0, vAxis, v0);
            float3 c01 = MakeCornerF(axis, w, uAxis, u0, vAxis, v1);
            float3 c10 = MakeCornerF(axis, w, uAxis, u1, vAxis, v0);
            float3 c11 = MakeCornerF(axis, w, uAxis, u1, vAxis, v1);

            // Ordre des coins reproduisant le winding de l'ancien meshing par
            // voxel (validé face par face contre GetFaceCornerIndex) : sur Z,
            // le signe qui donne PatternA est inversé par rapport à X/Y.
            bool usePatternA = axis == 2 ? sign < 0 : sign > 0;

            // Toutes les cellules fusionnées ont la même AO : les coins du
            // rectangle reprennent les 4 niveaux de la cellule (index du + 2*dv).
            // Eau : on garde son alpha (transparence), pas d'AO.
            byte a00 = isWater ? color.a : AoLevelToAlpha(ao & 3);
            byte a10 = isWater ? color.a : AoLevelToAlpha((ao >> 2) & 3);
            byte a01 = isWater ? color.a : AoLevelToAlpha((ao >> 4) & 3);
            byte a11 = isWater ? color.a : AoLevelToAlpha((ao >> 6) & 3);

            if (usePatternA)
            {
                AddGreedyFace(c00, c01, c10, c11, a00, a01, a10, a11, normal, color, isWater);
            }
            else
            {
                AddGreedyFace(c10, c11, c00, c01, a10, a11, a00, a01, normal, color, isWater);
            }
        }

        private void AddGreedyFace(
            float3 c0,
            float3 c1,
            float3 c2,
            float3 c3,
            byte ao0,
            byte ao1,
            byte ao2,
            byte ao3,
            float3 normal,
            Color32 color,
            bool isWater)
        {
            NativeList<float3> vertices = isWater ? WaterVertices : OpaqueVertices;
            NativeList<float3> normals = isWater ? WaterNormals : OpaqueNormals;
            NativeList<Color32> colors = isWater ? WaterColors : OpaqueColors;
            NativeList<int> triangles = isWater ? WaterTriangles : OpaqueTriangles;

            int baseIndex = vertices.Length;

            vertices.Add(c0);
            vertices.Add(c1);
            vertices.Add(c2);
            vertices.Add(c3);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            colors.Add(new Color32(color.r, color.g, color.b, ao0));
            colors.Add(new Color32(color.r, color.g, color.b, ao1));
            colors.Add(new Color32(color.r, color.g, color.b, ao2));
            colors.Add(new Color32(color.r, color.g, color.b, ao3));

            // Deux triangles. Les coins 0/3 et 1/2 sont opposés : l'AO étant
            // interpolée par triangle, on coupe le quad selon la diagonale
            // qui relie les coins les plus clairs (sinon une "croix" sombre
            // apparaît dans les angles — même règle que VoxelGridMesher).
            if (ao0 + ao3 > ao1 + ao2)
            {
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }
            else
            {
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
            }
        }

        private static int3 MakeCoord(int axis, int wVal, int uAxis, int uVal, int vAxis, int vVal)
        {
            var pos = default(int3);
            SetComponent(ref pos, axis, wVal);
            SetComponent(ref pos, uAxis, uVal);
            SetComponent(ref pos, vAxis, vVal);
            return pos;
        }

        private static float3 MakeCornerF(int axis, int w, int uAxis, int uVal, int vAxis, int vVal)
        {
            var pos = default(float3);
            SetComponentF(ref pos, axis, w);
            SetComponentF(ref pos, uAxis, uVal);
            SetComponentF(ref pos, vAxis, vVal);
            return pos;
        }

        private static void SetComponent(ref int3 v, int axis, int value)
        {
            switch (axis)
            {
                case 0:
                    v.x = value;
                    break;
                case 1:
                    v.y = value;
                    break;
                default:
                    v.z = value;
                    break;
            }
        }

        private static void SetComponentF(ref float3 v, int axis, float value)
        {
            switch (axis)
            {
                case 0:
                    v.x = value;
                    break;
                case 1:
                    v.y = value;
                    break;
                default:
                    v.z = value;
                    break;
            }
        }

        // Plantation de l'herbe/fleurs : indépendante du greedy meshing
        // ci-dessus (une touffe est ancrée à un voxel précis, pas à un
        // rectangle fusionné). Ne regarde que la face du dessus, seule
        // pertinente pour savoir si un voxel Grass est à l'air libre.
        private void PlantFoliage()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Voxel voxel = Voxels[ToIndex(x, y, z)];
                        if (voxel.Type != VoxelType.Grass)
                        {
                            continue;
                        }

                        var local = new int3(x, y, z);
                        Voxel top = SampleVoxel(local + new int3(0, 1, 0));
                        if (!IsFaceVisible(false, top))
                        {
                            continue;
                        }

                        int3 worldPos = Origin + local;
                        BiomeType biome = SampleBiome(worldPos.x, worldPos.z);

                        // L'herbe ne pousse que là où un voxel Grass est réellement à
                        // l'air libre — VoxelType.Grass n'existe déjà que sur Forêt/
                        // Plaines/Marais.
                        TryAddGrassTuft(local, biome);
                        if (biome == BiomeType.Plains)
                        {
                            TryAddFlower(local);
                        }
                    }
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
    }
}
