using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Construit quelques meshes d'arbre une seule fois (tronc + houppier, en
    /// mini-cubes via <see cref="VoxelCubeMeshBuilder"/>), partagés par toutes
    /// les instances placées par <see cref="VegetationSpawner"/> — évite de
    /// reconstruire un mesh à chaque arbre avec des dizaines de chunks Forêt
    /// chargés simultanément.
    /// </summary>
    internal static class TreeMeshLibrary
    {
        private static readonly Color32 TrunkColor = new(120, 74, 42, 255);
        private static readonly Color32[] CanopyColors =
        {
            new(46, 158, 53, 255),
            new(54, 168, 60, 255),
            new(40, 148, 48, 255),
            new(58, 172, 66, 255),
        };

        // Accessible à VegetationSpawner pour dimensionner le BoxCollider du tronc
        // sans dupliquer ce nombre.
        internal const int TrunkWidth = 2;

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

        private static Mesh BuildVariant(VegetationConfig config, ref Random rng, int variantIndex)
        {
            var mesh = new VoxelCubeMeshData();
            int variantSeed = (int)rng.NextUInt();

            int trunkHeight = rng.NextInt(config.TrunkHeightMin, config.TrunkHeightMax + 1);
            int canopyRadius = rng.NextInt(config.CanopyRadiusMin, config.CanopyRadiusMax + 1);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-TrunkWidth / 2, 0, -TrunkWidth / 2),
                TrunkWidth,
                trunkHeight,
                TrunkWidth,
                VoxelCubeShape.Column,
                TrunkColor,
                config.TreeVoxelUnit,
                variantSeed
            );

            Color32 canopyColor = CanopyColors[variantIndex % CanopyColors.Length];
            int canopySize = canopyRadius * 2;
            // Le houppier chevauche le haut du tronc plutôt que de reposer dessus.
            int canopyOriginY = trunkHeight - (canopyRadius / 2);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-canopyRadius, canopyOriginY, -canopyRadius),
                canopySize,
                canopySize,
                canopySize,
                VoxelCubeShape.Sphere,
                canopyColor,
                config.TreeVoxelUnit,
                variantSeed + 1
            );

            return mesh.ToMesh($"TreeVariant_{variantIndex}");
        }
    }
}
