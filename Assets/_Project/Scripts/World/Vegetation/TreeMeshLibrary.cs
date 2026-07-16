using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Construit des meshes d'arbre variés (tronc volumineux + houppier en
    /// grappes aléatoires collées au sommet), partagés par <see cref="VegetationSpawner"/>.
    /// </summary>
    internal static class TreeMeshLibrary
    {
        // Marrons du bois : cœur clair → écorce foncée, plus un dégradé vertical.
        private static readonly Color32 WoodCore = new(148, 98, 58, 255);
        private static readonly Color32 WoodMid = new(118, 74, 42, 255);
        private static readonly Color32 WoodBark = new(82, 52, 30, 255);
        private static readonly Color32 WoodRoot = new(96, 62, 36, 255);

        private static readonly Color32[] CanopyColors =
        {
            new(48, 152, 55, 255),
            new(42, 138, 50, 255),
            new(56, 162, 58, 255),
            new(38, 128, 46, 255),
            new(62, 170, 70, 255),
            new(34, 120, 42, 255),
        };

        // Demi-largeur max du tronc à la base (pour le BoxCollider).
        internal const int TrunkWidth = 4;

        public static Mesh[] BuildVariants(VegetationConfig config, int seed)
        {
            var meshes = new Mesh[Mathf.Max(1, config.TreeVariantCount)];
            var rng = new Random(math.max(1u, (uint)seed));

            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i] = BuildVariant(config, ref rng, i);
            }

            return meshes;
        }

        // Trois archétypes cyclés pour lisibilité Forêt : large, haut, clairière.
        private enum TreeArchetype
        {
            Broad = 0,
            Tall = 1,
            Clearing = 2,
        }

        private static Mesh BuildVariant(VegetationConfig config, ref Random rng, int variantIndex)
        {
            var mesh = new VoxelCubeMeshData();
            var archetype = (TreeArchetype)(variantIndex % 3);

            int trunkHeightMin = config.TrunkHeightMin;
            int trunkHeightMax = config.TrunkHeightMax;
            int canopyMin = config.CanopyRadiusMin;
            int canopyMax = config.CanopyRadiusMax;
            int baseRadiusMin = 2;
            int baseRadiusMax = 4;
            float leanMax = 0.55f;

            switch (archetype)
            {
                case TreeArchetype.Broad:
                    trunkHeightMin = math.max(8, config.TrunkHeightMin - 2);
                    trunkHeightMax = math.max(trunkHeightMin + 1, config.TrunkHeightMax - 2);
                    canopyMin = config.CanopyRadiusMin + 1;
                    canopyMax = config.CanopyRadiusMax + 2;
                    baseRadiusMin = 3;
                    baseRadiusMax = 5;
                    leanMax = 0.35f;
                    break;
                case TreeArchetype.Tall:
                    trunkHeightMin = config.TrunkHeightMin + 3;
                    trunkHeightMax = config.TrunkHeightMax + 6;
                    canopyMin = math.max(4, config.CanopyRadiusMin - 1);
                    canopyMax = math.max(canopyMin + 1, config.CanopyRadiusMax - 1);
                    baseRadiusMin = 2;
                    baseRadiusMax = 3;
                    leanMax = 0.45f;
                    break;
                default: // Clearing
                    trunkHeightMin = math.max(6, config.TrunkHeightMin - 4);
                    trunkHeightMax = math.max(trunkHeightMin + 1, config.TrunkHeightMax - 6);
                    canopyMin = math.max(3, config.CanopyRadiusMin - 2);
                    canopyMax = math.max(canopyMin + 1, config.CanopyRadiusMax - 2);
                    baseRadiusMin = 2;
                    baseRadiusMax = 3;
                    leanMax = 0.65f;
                    break;
            }

            int trunkHeight = rng.NextInt(trunkHeightMin, trunkHeightMax + 1);
            int baseRadius = rng.NextInt(baseRadiusMin, baseRadiusMax + 1);
            int topRadius = 1;

            float leanAngle = rng.NextFloat(-leanMax, leanMax);
            float leanAxis = rng.NextFloat(0f, math.PI * 2f);
            float leanX = math.cos(leanAxis) * leanAngle;
            float leanZ = math.sin(leanAxis) * leanAngle;
            float wobbleAmp = rng.NextFloat(0.15f, 0.45f);
            float wobblePhase = rng.NextFloat(0f, math.PI * 2f);

            int trunkPad = baseRadius + 2;
            int trunkSizeX = (trunkPad * 2) + 1;
            int trunkSizeZ = (trunkPad * 2) + 1;

            VoxelCubeMeshBuilder.AddCustom(
                mesh,
                new Vector3Int(-trunkPad, 0, -trunkPad),
                trunkSizeX,
                trunkHeight,
                trunkSizeZ,
                (x, y, z) =>
                    IsTrunkSolid(
                        x,
                        y,
                        z,
                        trunkPad,
                        trunkHeight,
                        baseRadius,
                        topRadius,
                        leanX,
                        leanZ,
                        wobbleAmp,
                        wobblePhase
                    ),
                (x, y, z) =>
                    TrunkColorAt(
                        x,
                        y,
                        z,
                        trunkPad,
                        trunkHeight,
                        baseRadius,
                        topRadius,
                        leanX,
                        leanZ,
                        wobbleAmp,
                        wobblePhase
                    ),
                config.TreeVoxelUnit
            );

            // Sommet réel du tronc (lean + wobble) — le houppier s'ancre ici.
            float3 tip = TrunkCenterAtHeight(
                trunkHeight - 1,
                trunkHeight,
                leanX,
                leanZ,
                wobbleAmp,
                wobblePhase
            );

            Color32 canopyBase = CanopyColors[rng.NextInt(0, CanopyColors.Length)];
            int canopyRadius = rng.NextInt(canopyMin, canopyMax + 1);
            AddRandomCanopy(
                mesh,
                ref rng,
                tip,
                trunkHeight,
                canopyRadius,
                canopyBase,
                config.TreeVoxelUnit,
                variantIndex
            );

            return mesh.ToMesh($"TreeVariant_{variantIndex}");
        }

        private static float3 TrunkCenterAtHeight(
            int y,
            int trunkHeight,
            float leanX,
            float leanZ,
            float wobbleAmp,
            float wobblePhase
        )
        {
            float heightNorm = trunkHeight <= 1 ? 0f : y / (float)(trunkHeight - 1);
            float cx = (leanX * heightNorm * trunkHeight * 0.35f)
                + (math.sin((heightNorm * math.PI * 2f) + wobblePhase) * wobbleAmp);
            float cz = (leanZ * heightNorm * trunkHeight * 0.35f)
                + (math.cos((heightNorm * math.PI * 1.5f) + wobblePhase) * wobbleAmp);
            return new float3(cx, y, cz);
        }

        private static bool IsTrunkSolid(
            int x,
            int y,
            int z,
            int pad,
            int trunkHeight,
            int baseRadius,
            int topRadius,
            float leanX,
            float leanZ,
            float wobbleAmp,
            float wobblePhase
        )
        {
            float t = trunkHeight <= 1 ? 0f : y / (float)(trunkHeight - 1);
            float radius = math.lerp(baseRadius, topRadius, t);

            if (y <= 1)
            {
                radius += 0.65f;
            }
            else if (y <= 3)
            {
                radius += 0.25f;
            }

            float3 center = TrunkCenterAtHeight(y, trunkHeight, leanX, leanZ, wobbleAmp, wobblePhase);
            float dx = x - (pad + center.x);
            float dz = z - (pad + center.z);
            return ((dx * dx) + (dz * dz)) <= (radius * radius);
        }

        // Dégradé marron : racines / cœur / écorce + anneaux verticaux doux.
        private static Color32 TrunkColorAt(
            int x,
            int y,
            int z,
            int pad,
            int trunkHeight,
            int baseRadius,
            int topRadius,
            float leanX,
            float leanZ,
            float wobbleAmp,
            float wobblePhase
        )
        {
            float t = trunkHeight <= 1 ? 0f : y / (float)(trunkHeight - 1);
            float radius = math.lerp(baseRadius, topRadius, t);
            if (y <= 1)
            {
                radius += 0.65f;
            }
            else if (y <= 3)
            {
                radius += 0.25f;
            }

            float3 center = TrunkCenterAtHeight(y, trunkHeight, leanX, leanZ, wobbleAmp, wobblePhase);
            float dx = x - (pad + center.x);
            float dz = z - (pad + center.z);
            float radial = radius > 0.01f
                ? math.saturate(math.sqrt((dx * dx) + (dz * dz)) / radius)
                : 0f;

            // Vertical : racines plus chaudes en bas, bois plus sombre sous le feuillage.
            Color32 vertical = LerpColor(WoodRoot, WoodBark, t);
            // Radial : cœur clair → écorce foncée.
            Color32 wood = LerpColor(WoodCore, LerpColor(WoodMid, vertical, 0.55f), radial);

            // Anneaux / stries verticales douces (pas un damier voxel).
            float rings = math.sin((t * 14f) + (radial * 3.5f)) * 0.5f + 0.5f;
            float shade = math.lerp(0.9f, 1.08f, rings);
            // Face « ombrée » côté lean pour un peu de volume couleur.
            float side = math.saturate((dx * leanX) + (dz * leanZ) + 0.5f);
            shade *= math.lerp(0.92f, 1.05f, side);

            return Scale(wood, shade);
        }

        private static void AddRandomCanopy(
            VoxelCubeMeshData mesh,
            ref Random rng,
            float3 tip,
            int trunkHeight,
            int canopyRadius,
            Color32 canopyBase,
            float unit,
            int variantIndex
        )
        {
            int clusterCount = rng.NextInt(4, 8); // 4 à 7 grappes
            int pad = canopyRadius + 3;
            int size = (pad * 2) + 1;

            // Centre logique du volume feuilles = sommet du tronc (chevauchement).
            int tipY = trunkHeight - 1;
            int canopyOriginY = tipY - pad;

            var clusters = new CanopyCluster[clusterCount];

            // Grappe centrale collée au tronc (obligatoire).
            float coreRadius = canopyRadius * rng.NextFloat(0.55f, 0.75f);
            clusters[0] = new CanopyCluster(
                tip.x,
                0f, // au niveau du tip dans l'espace local du houppier
                tip.z,
                coreRadius,
                rng.NextFloat(0.75f, 1.05f)
            );

            for (int i = 1; i < clusterCount; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(0.15f, canopyRadius * 0.7f);
                float radius = rng.NextFloat(canopyRadius * 0.35f, canopyRadius * 0.7f);
                // Reste majoritairement au-dessus / autour du tip, chevauche le tronc.
                float yOff = rng.NextFloat(-coreRadius * 0.25f, canopyRadius * 0.45f);
                float flatten = rng.NextFloat(0.6f, 1.1f);

                clusters[i] = new CanopyCluster(
                    tip.x + (math.cos(angle) * dist),
                    yOff,
                    tip.z + (math.sin(angle) * dist),
                    radius,
                    flatten
                );
            }

            uint holeSeed = (uint)(variantIndex * 9973) ^ rng.NextUInt();

            VoxelCubeMeshBuilder.AddCustom(
                mesh,
                new Vector3Int(-pad, canopyOriginY, -pad),
                size,
                size,
                size,
                (x, y, z) => IsCanopySolid(x, y, z, pad, clusters, holeSeed),
                (x, y, z) => CanopyColorAt(canopyBase, x, y, z, size, clusters, pad),
                unit
            );
        }

        private static bool IsCanopySolid(
            int x,
            int y,
            int z,
            int pad,
            CanopyCluster[] clusters,
            uint holeSeed
        )
        {
            float wx = x - pad;
            float wy = y - pad;
            float wz = z - pad;

            bool inside = false;
            for (int i = 0; i < clusters.Length; i++)
            {
                CanopyCluster c = clusters[i];
                float dx = (wx - c.X) / c.Radius;
                float dy = (wy - c.Y) / (c.Radius * c.Flatten);
                float dz = (wz - c.Z) / c.Radius;
                if (((dx * dx) + (dy * dy) + (dz * dz)) <= 1f)
                {
                    inside = true;
                    break;
                }
            }

            if (!inside)
            {
                return false;
            }

            // Moins de trous près du centre (grappe 0) pour rester collé au tronc.
            float distToCore = math.sqrt((wx - clusters[0].X) * (wx - clusters[0].X)
                + (wy - clusters[0].Y) * (wy - clusters[0].Y)
                + (wz - clusters[0].Z) * (wz - clusters[0].Z));
            float holeChance = distToCore < clusters[0].Radius * 0.55f ? 0.03f : 0.12f;

            uint h = math.hash(new int4(x, y, z, (int)holeSeed));
            float u = (h & 0x00FFFFFFu) / (float)0x01000000u;
            return u > holeChance;
        }

        private static Color32 CanopyColorAt(
            Color32 baseColor,
            int x,
            int y,
            int z,
            int size,
            CanopyCluster[] clusters,
            int pad
        )
        {
            float wx = x - pad;
            float wy = y - pad;
            float wz = z - pad;

            float heightT = size <= 1 ? 0.5f : y / (float)(size - 1);
            float nearestEdge = 1f;
            for (int i = 0; i < clusters.Length; i++)
            {
                CanopyCluster c = clusters[i];
                float dx = (wx - c.X) / c.Radius;
                float dy = (wy - c.Y) / (c.Radius * c.Flatten);
                float dz = (wz - c.Z) / c.Radius;
                float d = math.sqrt((dx * dx) + (dy * dy) + (dz * dz));
                nearestEdge = math.min(nearestEdge, d);
            }

            float factor = math.lerp(0.84f, 1.12f, heightT);
            factor *= math.lerp(1.06f, 0.9f, math.saturate(nearestEdge));
            return Scale(baseColor, factor);
        }

        private static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
            t = math.saturate(t);
            return new Color32(
                (byte)math.clamp((int)math.lerp(a.r, b.r, t), 0, 255),
                (byte)math.clamp((int)math.lerp(a.g, b.g, t), 0, 255),
                (byte)math.clamp((int)math.lerp(a.b, b.b, t), 0, 255),
                255
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

        private readonly struct CanopyCluster
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Z;
            public readonly float Radius;
            public readonly float Flatten;

            public CanopyCluster(float x, float y, float z, float radius, float flatten)
            {
                X = x;
                Y = y;
                Z = z;
                Radius = radius;
                Flatten = flatten;
            }
        }
    }
}
