using System.Collections.Generic;
using CubeWorld.Core;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Place des arbres sur les colonnes Grass en biome Forêt, à mesure que les
    /// chunks streament — même pattern d'abonnement que
    /// <see cref="NavMeshRegionBaker"/> (EventBus, pas d'appel direct depuis
    /// WorldBootstrap). Chaque chunk n'est traité qu'une seule fois (à sa
    /// première matérialisation) : un remesh dû au voisinage ne change jamais
    /// le terrain lui-même, donc jamais les arbres qui y poussent.
    /// </summary>
    public sealed class VegetationSpawner : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField]
        private WorldBootstrap _worldBootstrap;

        [SerializeField]
        private WorldConfig _config;

        [SerializeField]
        private VegetationConfig _vegetationConfig;

        [Tooltip(
            "Rayon (en voxels) autour du point de spawn du joueur (0, 0) où aucun arbre n'apparaît."
        )]
        [SerializeField]
        private int _spawnExclusionRadius = 6;

        private VegetationConfig vegetationConfig;
        private Mesh[] treeVariants;
        private Material treeMaterial;
        private readonly Dictionary<int3, GameObject> vegetationRoots = new();
        private readonly List<Vector2Int> placedThisChunkBuffer = new();

        private void Awake()
        {
            vegetationConfig =
                _vegetationConfig != null
                    ? _vegetationConfig
                    : ScriptableObject.CreateInstance<VegetationConfig>();

            treeVariants = TreeMeshLibrary.BuildVariants(
                vegetationConfig,
                _config != null ? _config.Seed : 1
            );
            treeMaterial = CreateTreeMaterial();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ChunkMeshMaterializedEvent>(OnChunkMeshMaterialized);
            EventBus.Subscribe<ChunkUnloadedEvent>(OnChunkUnloaded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ChunkMeshMaterializedEvent>(OnChunkMeshMaterialized);
            EventBus.Unsubscribe<ChunkUnloadedEvent>(OnChunkUnloaded);
        }

        private void OnChunkMeshMaterialized(ChunkMeshMaterializedEvent evt)
        {
            var chunkCoord = new int3(evt.ChunkCoord.x, evt.ChunkCoord.y, evt.ChunkCoord.z);

            if (
                vegetationRoots.ContainsKey(chunkCoord)
                || _worldBootstrap == null
                || _config == null
            )
            {
                return;
            }

            var root = new GameObject(
                $"Vegetation ({chunkCoord.x}, {chunkCoord.y}, {chunkCoord.z})"
            );
            // Parenté au transform de WorldBootstrap (le référentiel réel du monde,
            // celui qui porte aussi les chunks de terrain), pas à celui de ce
            // composant : si le GameObject VegetationSpawner est un jour déplacé
            // dans l'éditeur, les arbres restent alignés avec le terrain au lieu
            // d'hériter d'un décalage parasite.
            root.transform.SetParent(_worldBootstrap.transform, false);
            vegetationRoots[chunkCoord] = root;

            PlaceTrees(chunkCoord, root);
        }

        private void OnChunkUnloaded(ChunkUnloadedEvent evt)
        {
            var chunkCoord = new int3(evt.ChunkCoord.x, evt.ChunkCoord.y, evt.ChunkCoord.z);

            if (vegetationRoots.Remove(chunkCoord, out GameObject root))
            {
                Destroy(root);
            }
        }

        private void PlaceTrees(int3 chunkCoord, GameObject root)
        {
            VoxelWorld world = _worldBootstrap.World;
            if (world == null || treeVariants.Length == 0)
            {
                return;
            }

            int chunkSize = _config.ChunkSize;
            int3 voxelOrigin = chunkCoord * chunkSize;

            placedThisChunkBuffer.Clear();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = voxelOrigin.x + x;
                    int worldZ = voxelOrigin.z + z;

                    // Filtre bon marché en premier (rayon d'exclusion + densité,
                    // aucune lecture de voxel) : la grande majorité des colonnes
                    // s'arrêtent ici, avant même de lire les voxels générés.
                    if (!PassesDensityGate(worldX, worldZ))
                    {
                        continue;
                    }

                    if (
                        !TryFindGrassSurfaceInChunk(
                            world,
                            worldX,
                            worldZ,
                            voxelOrigin.y,
                            chunkSize,
                            out int surfaceHeight
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        !IsSpacingOk(worldX, worldZ)
                        || world.GetBiome(worldX, worldZ) != BiomeType.Forest
                    )
                    {
                        continue;
                    }

                    placedThisChunkBuffer.Add(new Vector2Int(worldX, worldZ));
                    SpawnTree(root, worldX, surfaceHeight, worldZ);
                }
            }
        }

        // Cherche un voxel Grass exposé dans les seules limites verticales de ce
        // chunk, en lisant directement les voxels déjà générés (world.GetVoxel,
        // un accès tableau) plutôt qu'en rééchantillonnant le bruit fractal du
        // terrain (world.GetSurfaceHeight) pour les 1024 colonnes de chaque
        // chunk matérialisé — bien trop coûteux sur le thread principal et
        // responsable d'un vrai temps de chargement ressenti. Descend depuis le
        // haut du chunk et s'arrête au premier voxel non-air (couche unique,
        // pas de surplomb possible dans ce générateur) : cette pile appartient
        // à un autre chunk vertical de la colonne dès qu'un solide non-Grass
        // apparaît (désert/neige/pierre) ou que le chunk entier est vide.
        private static bool TryFindGrassSurfaceInChunk(
            VoxelWorld world,
            int worldX,
            int worldZ,
            int chunkOriginY,
            int chunkSize,
            out int surfaceHeight
        )
        {
            for (int y = chunkSize - 1; y >= 0; y--)
            {
                int worldY = chunkOriginY + y;
                Voxel voxel = world.GetVoxel(new int3(worldX, worldY, worldZ));

                if (voxel.IsAir)
                {
                    continue;
                }

                if (voxel.Type == VoxelType.Grass)
                {
                    surfaceHeight = worldY;
                    return true;
                }

                break;
            }

            surfaceHeight = 0;
            return false;
        }

        // Aucune lecture de voxel : purement arithmétique, sûr d'appeler pour
        // les 1024 colonnes d'un chunk sans impact mesurable.
        private bool PassesDensityGate(int worldX, int worldZ)
        {
            if (worldX * worldX + worldZ * worldZ < _spawnExclusionRadius * _spawnExclusionRadius)
            {
                return false;
            }

            return HashToUnit(worldX, worldZ, 0) < vegetationConfig.TreeDensity;
        }

        private bool IsSpacingOk(int worldX, int worldZ)
        {
            int minSpacingSq = vegetationConfig.TreeMinSpacing * vegetationConfig.TreeMinSpacing;

            foreach (Vector2Int placed in placedThisChunkBuffer)
            {
                int dx = placed.x - worldX;
                int dz = placed.y - worldZ;
                if ((dx * dx) + (dz * dz) < minSpacingSq)
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnTree(GameObject root, int worldX, int surfaceHeight, int worldZ)
        {
            int variantIndex = (int)(HashToUnit(worldX, worldZ, 1) * treeVariants.Length);
            variantIndex = Mathf.Clamp(variantIndex, 0, treeVariants.Length - 1);
            float rotationY = HashToUnit(worldX, worldZ, 2) * 360f;

            var treeObject = new GameObject($"Tree ({worldX}, {surfaceHeight}, {worldZ})");
            treeObject.transform.SetParent(root.transform, false);
            // Position convertie en unités monde (voir WorldBootstrap.CreateChunkVisual) ;
            // le mesh lui-même est déjà à sa taille finale (TreeMeshLibrary utilise
            // TreeVoxelUnit, indépendant de VoxelSize) — pas d'échelle supplémentaire ici.
            treeObject.transform.localPosition =
                new Vector3(worldX, surfaceHeight + 1, worldZ) * _config.VoxelSize;
            treeObject.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

            treeObject.AddComponent<MeshFilter>().sharedMesh = treeVariants[variantIndex];
            treeObject.AddComponent<MeshRenderer>().sharedMaterial = treeMaterial;

            // Collider simple sur le tronc seulement : bloque le joueur sans le
            // coût d'un MeshCollider sur un houppier jamais destiné à être miné.
            float trunkWidth = TreeMeshLibrary.TrunkWidth * vegetationConfig.TreeVoxelUnit;
            float trunkHeight = vegetationConfig.TrunkHeightMax * vegetationConfig.TreeVoxelUnit;
            BoxCollider trunkCollider = treeObject.AddComponent<BoxCollider>();
            trunkCollider.size = new Vector3(trunkWidth, trunkHeight, trunkWidth);
            trunkCollider.center = new Vector3(0f, trunkHeight * 0.5f, 0f);
        }

        private static Material CreateTreeMaterial()
        {
            Shader shader = Shader.Find("CubeWorld/VoxelTerrain");
            return shader != null ? new Material(shader) : null;
        }

        // Hash déterministe [0, 1), même principe que VoxelPalette/ChunkMeshBuildJob.
        private static float HashToUnit(int x, int z, int salt)
        {
            unchecked
            {
                uint h = (uint)salt * 374761393u;
                h ^= (uint)x * 668265263u;
                h ^= (uint)z * 2246822519u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0x00FFFFFFu) / (float)0x01000000u;
            }
        }
    }
}
