using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Meshes de props procéduraux (désert / neige / marais), partagés par
    /// <see cref="VegetationSpawner"/>. Même pipeline mini-cubes que les arbres.
    /// </summary>
    internal static class PropMeshLibrary
    {
        internal readonly struct PropVariant
        {
            public readonly Mesh Mesh;
            public readonly float ColliderWidth;
            public readonly float ColliderHeight;
            public readonly bool HasCollider;
            /// <summary>Si faux, plante à la hauteur de surface (pas le min voisin) — évite d'enterrer les petits props.</summary>
            public readonly bool UseSlopePlant;

            public PropVariant(
                Mesh mesh,
                float colliderWidth,
                float colliderHeight,
                bool hasCollider,
                bool useSlopePlant = true
            )
            {
                Mesh = mesh;
                ColliderWidth = colliderWidth;
                ColliderHeight = colliderHeight;
                HasCollider = hasCollider;
                UseSlopePlant = useSlopePlant;
            }
        }

        private static readonly Color32 CactusGreen = new(62, 148, 72, 255);
        private static readonly Color32 CactusDark = new(48, 118, 58, 255);
        private static readonly Color32 RockWarm = new(168, 132, 88, 255);
        private static readonly Color32 RockCool = new(130, 138, 148, 255);
        private static readonly Color32 SnowCap = new(245, 250, 255, 255);
        private static readonly Color32 IceBlue = new(180, 220, 240, 255);
        private static readonly Color32 PineGreen = new(36, 98, 52, 255);
        private static readonly Color32 PineDark = new(28, 78, 42, 255);
        private static readonly Color32 WoodDark = new(72, 48, 28, 255);
        private static readonly Color32 SwampCanopy = new(28, 68, 32, 255);
        private static readonly Color32 ReedGreen = new(48, 88, 40, 255);
        private static readonly Color32 MushroomCap = new(196, 72, 62, 255);
        private static readonly Color32 MushroomStem = new(230, 220, 200, 255);

        public static PropVariant[] BuildDesertVariants(VegetationConfig config, int seed)
        {
            int count = Mathf.Max(1, config.DesertPropVariantCount);
            var variants = new PropVariant[count];
            var rng = new Random(math.max(1u, (uint)seed ^ 0xD35E17u));
            float unit = config.PropVoxelUnit;

            for (int i = 0; i < count; i++)
            {
                variants[i] = (i % 3) switch
                {
                    0 => BuildCactus(ref rng, unit, i),
                    1 => BuildCactus(ref rng, unit, i),
                    _ => BuildRockPile(ref rng, unit, i, warm: true),
                };
            }

            return variants;
        }

        public static PropVariant[] BuildSnowVariants(VegetationConfig config, int seed)
        {
            int count = Mathf.Max(1, config.SnowPropVariantCount);
            var variants = new PropVariant[count];
            var rng = new Random(math.max(1u, (uint)seed ^ 0x51A1CEu));
            float unit = config.PropVoxelUnit;

            for (int i = 0; i < count; i++)
            {
                variants[i] = (i % 3) switch
                {
                    0 => BuildPine(ref rng, unit, i),
                    1 => BuildIceSpike(ref rng, unit, i),
                    _ => BuildRockPile(ref rng, unit, i, warm: false),
                };
            }

            return variants;
        }

        public static PropVariant[] BuildSwampVariants(VegetationConfig config, int seed)
        {
            int count = Mathf.Max(1, config.SwampPropVariantCount);
            var variants = new PropVariant[count];
            var rng = new Random(math.max(1u, (uint)seed ^ 0x5A4A50u));
            float unit = config.PropVoxelUnit;

            for (int i = 0; i < count; i++)
            {
                // Peu de champignons (~1/8 des variantes) : surtout arbres + roseaux.
                variants[i] = (i % 8) switch
                {
                    0 or 1 or 2 => BuildTwistedTree(ref rng, unit, i),
                    7 => BuildMushroom(ref rng, unit, i),
                    _ => BuildReed(ref rng, unit, i),
                };
            }

            return variants;
        }

        private static PropVariant BuildCactus(ref Random rng, float unit, int index)
        {
            var mesh = new VoxelCubeMeshData();
            int height = rng.NextInt(8, 14);
            int trunkW = rng.NextInt(2, 4);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-(trunkW / 2), 0, -(trunkW / 2)),
                trunkW,
                height,
                trunkW,
                VoxelCubeShape.Column,
                (x, y, z) => y > height - 2 ? CactusDark : CactusGreen,
                unit,
                index);

            // Bras latéraux occasionnels.
            if (rng.NextFloat() > 0.35f)
            {
                int armY = rng.NextInt(height / 3, (height * 2) / 3);
                int armLen = rng.NextInt(2, 4);
                int side = rng.NextFloat() > 0.5f ? 1 : -1;
                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(side > 0 ? trunkW / 2 : -armLen - (trunkW / 2), armY, -(trunkW / 2)),
                    armLen,
                    trunkW,
                    trunkW,
                    VoxelCubeShape.Box,
                    CactusGreen,
                    unit,
                    index + 11);
                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(
                        side > 0 ? (trunkW / 2) + armLen - trunkW : -armLen - (trunkW / 2),
                        armY,
                        -(trunkW / 2)),
                    trunkW,
                    rng.NextInt(2, 4),
                    trunkW,
                    VoxelCubeShape.Column,
                    CactusDark,
                    unit,
                    index + 12);
            }

            float colliderW = trunkW * unit * 1.1f;
            float colliderH = height * unit;
            return new PropVariant(mesh.ToMesh($"DesertCactus_{index}"), colliderW, colliderH, true);
        }

        private static PropVariant BuildRockPile(ref Random rng, float unit, int index, bool warm)
        {
            var mesh = new VoxelCubeMeshData();
            Color32 rock = warm ? RockWarm : RockCool;
            int blocks = rng.NextInt(2, 5);
            int maxH = 1;

            for (int b = 0; b < blocks; b++)
            {
                int sx = rng.NextInt(2, 5);
                int sy = rng.NextInt(1, 4);
                int sz = rng.NextInt(2, 5);
                maxH = math.max(maxH, sy);
                int ox = rng.NextInt(-2, 3);
                int oz = rng.NextInt(-2, 3);
                Color32 color = Scale(rock, rng.NextFloat(0.88f, 1.08f));

                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(ox - (sx / 2), 0, oz - (sz / 2)),
                    sx,
                    sy,
                    sz,
                    VoxelCubeShape.Sphere,
                    color,
                    unit,
                    index + b);

                if (!warm && rng.NextFloat() > 0.45f)
                {
                    VoxelCubeMeshBuilder.AddPart(
                        mesh,
                        new Vector3Int(ox - (sx / 2), sy, oz - (sz / 2)),
                        sx,
                        1,
                        sz,
                        VoxelCubeShape.Box,
                        SnowCap,
                        unit,
                        index + 40 + b);
                    maxH = math.max(maxH, sy + 1);
                }
            }

            float colliderW = 4f * unit;
            float colliderH = maxH * unit;
            string name = warm ? $"DesertRock_{index}" : $"SnowRock_{index}";
            return new PropVariant(mesh.ToMesh(name), colliderW, colliderH, true);
        }

        private static PropVariant BuildPine(ref Random rng, float unit, int index)
        {
            // Sapins nettement plus grands que les autres props neige (×3).
            float pineUnit = unit * 3f;
            var mesh = new VoxelCubeMeshData();
            int trunkH = rng.NextInt(8, 14);
            int trunkW = 2;

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-1, 0, -1),
                trunkW,
                trunkH,
                trunkW,
                VoxelCubeShape.Column,
                WoodDark,
                pineUnit,
                index);

            int layers = rng.NextInt(4, 6);
            int y = trunkH - 1;
            for (int layer = 0; layer < layers; layer++)
            {
                int radius = math.max(2, layers - layer + 2);
                int size = (radius * 2) + 1;
                int layerH = rng.NextInt(2, 4);
                Color32 color = Scale(layer % 2 == 0 ? PineGreen : PineDark, rng.NextFloat(0.92f, 1.05f));

                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(-radius, y, -radius),
                    size,
                    layerH,
                    size,
                    VoxelCubeShape.Sphere,
                    color,
                    pineUnit,
                    index + layer);

                y += layerH - 1;
            }

            float colliderW = 3f * pineUnit;
            float colliderH = (y + 2) * pineUnit;
            return new PropVariant(mesh.ToMesh($"SnowPine_{index}"), colliderW, colliderH, true);
        }

        private static PropVariant BuildIceSpike(ref Random rng, float unit, int index)
        {
            var mesh = new VoxelCubeMeshData();
            int height = rng.NextInt(6, 12);
            int baseW = rng.NextInt(2, 4);

            VoxelCubeMeshBuilder.AddCustom(
                mesh,
                new Vector3Int(-(baseW), 0, -(baseW)),
                (baseW * 2) + 1,
                height,
                (baseW * 2) + 1,
                (x, y, z) =>
                {
                    float t = height <= 1 ? 0f : y / (float)(height - 1);
                    float radius = math.lerp(baseW, 0.4f, t);
                    float dx = x - baseW;
                    float dz = z - baseW;
                    return ((dx * dx) + (dz * dz)) <= (radius * radius);
                },
                (x, y, z) => Scale(IceBlue, 0.9f + (y / (float)height) * 0.2f),
                unit);

            float colliderW = baseW * unit;
            float colliderH = height * unit;
            return new PropVariant(mesh.ToMesh($"IceSpike_{index}"), colliderW, colliderH, true);
        }

        private static PropVariant BuildTwistedTree(ref Random rng, float unit, int index)
        {
            var mesh = new VoxelCubeMeshData();
            int trunkH = rng.NextInt(8, 14);
            float lean = rng.NextFloat(0.7f, 1.2f) * (rng.NextFloat() > 0.5f ? 1f : -1f);
            float leanZ = rng.NextFloat(-0.5f, 0.5f);
            int pad = 5;

            VoxelCubeMeshBuilder.AddCustom(
                mesh,
                new Vector3Int(-pad, 0, -pad),
                (pad * 2) + 1,
                trunkH,
                (pad * 2) + 1,
                (x, y, z) =>
                {
                    float t = trunkH <= 1 ? 0f : y / (float)(trunkH - 1);
                    float cx = pad + (lean * t * trunkH * 0.25f);
                    float cz = pad + (leanZ * t * trunkH * 0.25f);
                    float radius = math.lerp(2.2f, 1f, t);
                    float dx = x - cx;
                    float dz = z - cz;
                    return ((dx * dx) + (dz * dz)) <= (radius * radius);
                },
                (_, y, _) => Scale(WoodDark, 0.85f + (y / (float)trunkH) * 0.2f),
                unit);

            int canopyR = rng.NextInt(3, 6);
            int tipX = (int)(lean * 0.25f * trunkH);
            int tipZ = (int)(leanZ * 0.25f * trunkH);
            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(tipX - canopyR, trunkH - 2, tipZ - canopyR),
                (canopyR * 2) + 1,
                canopyR + 1,
                (canopyR * 2) + 1,
                VoxelCubeShape.Sphere,
                (x, y, z) =>
                {
                    uint h = math.hash(new int3(x, y, z));
                    return (h & 7) == 0 ? Scale(SwampCanopy, 0.75f) : SwampCanopy;
                },
                unit,
                index);

            float colliderW = 3f * unit;
            float colliderH = trunkH * unit;
            return new PropVariant(mesh.ToMesh($"SwampTree_{index}"), colliderW, colliderH, true);
        }

        private static PropVariant BuildReed(ref Random rng, float unit, int index)
        {
            var mesh = new VoxelCubeMeshData();
            int stalks = rng.NextInt(2, 5);

            for (int s = 0; s < stalks; s++)
            {
                int h = rng.NextInt(4, 8);
                int ox = rng.NextInt(-2, 3);
                int oz = rng.NextInt(-2, 3);
                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(ox, 0, oz),
                    1,
                    h,
                    1,
                    VoxelCubeShape.Box,
                    Scale(ReedGreen, rng.NextFloat(0.9f, 1.1f)),
                    unit,
                    index + s);
            }

            // Pas de plantage au min des voisins : les tiges restent au niveau du sol.
            return new PropVariant(mesh.ToMesh($"Reed_{index}"), 0f, 0f, hasCollider: false, useSlopePlant: false);
        }

        private static PropVariant BuildMushroom(ref Random rng, float unit, int index)
        {
            var mesh = new VoxelCubeMeshData();
            int stemH = rng.NextInt(3, 5);
            int capR = rng.NextInt(2, 4);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(0, 0, 0),
                1,
                stemH,
                1,
                VoxelCubeShape.Box,
                MushroomStem,
                unit,
                index);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-capR, stemH, -capR),
                (capR * 2) + 1,
                2,
                (capR * 2) + 1,
                VoxelCubeShape.Box,
                MushroomCap,
                unit,
                index + 1);

            return new PropVariant(
                mesh.ToMesh($"Mushroom_{index}"),
                0f,
                0f,
                hasCollider: false,
                useSlopePlant: false
            );
        }

        private static Color32 Scale(Color32 color, float factor)
        {
            return new Color32(
                (byte)math.clamp((int)(color.r * factor), 0, 255),
                (byte)math.clamp((int)(color.g * factor), 0, 255),
                (byte)math.clamp((int)(color.b * factor), 0, 255),
                color.a
            );
        }
    }
}
