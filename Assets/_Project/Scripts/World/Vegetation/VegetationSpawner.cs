using System.Collections.Generic;
using CubeWorld.Core;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Place la végétation / props par biome à mesure que les chunks streament —
    /// même pattern d'abonnement que <see cref="NavMeshRegionBaker"/> (EventBus).
    /// Chaque chunk n'est traité qu'une seule fois (à sa première matérialisation).
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
            "Rayon (en voxels) autour du point de spawn du joueur (0, 0) où aucun prop n'apparaît."
        )]
        [SerializeField]
        private int _spawnExclusionRadius = 6;

        private VegetationConfig vegetationConfig;
        private Material propMaterial;
        private PropFamily[] families;
        private readonly Dictionary<int3, GameObject> vegetationRoots = new();
        private readonly List<Vector2Int> placedThisChunkBuffer = new();

        private readonly struct PropFamily
        {
            public readonly BiomeType Biome;
            public readonly VoxelType SurfaceType;
            public readonly float Density;
            public readonly int MinSpacing;
            public readonly int DensitySalt;
            public readonly PropMeshLibrary.PropVariant[] Variants;

            public PropFamily(
                BiomeType biome,
                VoxelType surfaceType,
                float density,
                int minSpacing,
                int densitySalt,
                PropMeshLibrary.PropVariant[] variants
            )
            {
                Biome = biome;
                SurfaceType = surfaceType;
                Density = density;
                MinSpacing = minSpacing;
                DensitySalt = densitySalt;
                Variants = variants;
            }
        }

        private void Awake()
        {
            vegetationConfig =
                _vegetationConfig != null
                    ? _vegetationConfig
                    : ScriptableObject.CreateInstance<VegetationConfig>();

            propMaterial = CreatePropMaterial();
        }

        private void Start()
        {
            int seed =
                _worldBootstrap != null && _worldBootstrap.Config != null
                    ? _worldBootstrap.Config.Seed
                    : _config != null
                        ? _config.Seed
                        : 1;

            Mesh[] treeMeshes = TreeMeshLibrary.BuildVariants(vegetationConfig, seed);
            var forestVariants = new PropMeshLibrary.PropVariant[treeMeshes.Length];
            float trunkW = TreeMeshLibrary.TrunkWidth * vegetationConfig.TreeVoxelUnit;
            float trunkH = vegetationConfig.TrunkHeightMax * vegetationConfig.TreeVoxelUnit;
            for (int i = 0; i < treeMeshes.Length; i++)
            {
                forestVariants[i] = new PropMeshLibrary.PropVariant(
                    treeMeshes[i],
                    trunkW * 0.85f,
                    trunkH,
                    true
                );
            }

            families = new[]
            {
                new PropFamily(
                    BiomeType.Forest,
                    VoxelType.Grass,
                    vegetationConfig.TreeDensity,
                    vegetationConfig.TreeMinSpacing,
                    densitySalt: 0,
                    forestVariants
                ),
                new PropFamily(
                    BiomeType.Desert,
                    VoxelType.Sand,
                    vegetationConfig.DesertPropDensity,
                    vegetationConfig.DesertPropMinSpacing,
                    densitySalt: 100,
                    PropMeshLibrary.BuildDesertVariants(vegetationConfig, seed)
                ),
                new PropFamily(
                    BiomeType.Snow,
                    VoxelType.Snow,
                    vegetationConfig.SnowPropDensity,
                    vegetationConfig.SnowPropMinSpacing,
                    densitySalt: 200,
                    PropMeshLibrary.BuildSnowVariants(vegetationConfig, seed)
                ),
                new PropFamily(
                    BiomeType.Swamp,
                    VoxelType.Grass,
                    vegetationConfig.SwampPropDensity,
                    vegetationConfig.SwampPropMinSpacing,
                    densitySalt: 300,
                    PropMeshLibrary.BuildSwampVariants(vegetationConfig, seed)
                ),
            };
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
                || families == null
            )
            {
                return;
            }

            var root = new GameObject(
                $"Vegetation ({chunkCoord.x}, {chunkCoord.y}, {chunkCoord.z})"
            );
            root.transform.SetParent(_worldBootstrap.transform, false);
            vegetationRoots[chunkCoord] = root;

            for (int i = 0; i < families.Length; i++)
            {
                PlaceFamily(chunkCoord, root, families[i]);
            }
        }

        private void OnChunkUnloaded(ChunkUnloadedEvent evt)
        {
            var chunkCoord = new int3(evt.ChunkCoord.x, evt.ChunkCoord.y, evt.ChunkCoord.z);

            if (vegetationRoots.Remove(chunkCoord, out GameObject root))
            {
                Destroy(root);
            }
        }

        private void PlaceFamily(int3 chunkCoord, GameObject root, PropFamily family)
        {
            VoxelWorld world = _worldBootstrap.World;
            if (world == null || family.Variants == null || family.Variants.Length == 0)
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

                    if (!PassesDensityGate(worldX, worldZ, family.Density, family.DensitySalt))
                    {
                        continue;
                    }

                    if (
                        !TryFindSurfaceInChunk(
                            world,
                            worldX,
                            worldZ,
                            voxelOrigin.y,
                            chunkSize,
                            family.SurfaceType,
                            out int surfaceHeight
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        !IsSpacingOk(worldX, worldZ, family.MinSpacing)
                        || world.GetBiome(worldX, worldZ) != family.Biome
                    )
                    {
                        continue;
                    }

                    int variantIndex = (int)(
                        HashToUnit(worldX, worldZ, family.DensitySalt + 1) * family.Variants.Length
                    );
                    variantIndex = Mathf.Clamp(variantIndex, 0, family.Variants.Length - 1);
                    PropMeshLibrary.PropVariant variant = family.Variants[variantIndex];

                    int plantHeight = surfaceHeight;
                    if (
                        variant.UseSlopePlant
                        && !TryGetStablePlantHeight(
                            world,
                            worldX,
                            worldZ,
                            surfaceHeight,
                            out plantHeight
                        )
                    )
                    {
                        continue;
                    }

                    placedThisChunkBuffer.Add(new Vector2Int(worldX, worldZ));
                    SpawnProp(root, worldX, plantHeight, worldZ, family, variant);
                }
            }
        }

        private bool TryGetStablePlantHeight(
            VoxelWorld world,
            int worldX,
            int worldZ,
            int centerSurfaceHeight,
            out int plantHeight
        )
        {
            const int footprintRadius = 1;
            const int maxSlopeVoxels = 2;

            int minHeight = centerSurfaceHeight;
            int maxHeight = centerSurfaceHeight;

            for (int dz = -footprintRadius; dz <= footprintRadius; dz++)
            {
                for (int dx = -footprintRadius; dx <= footprintRadius; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int h = world.GetSurfaceHeight(worldX + dx, worldZ + dz);
                    if (h < minHeight)
                    {
                        minHeight = h;
                    }

                    if (h > maxHeight)
                    {
                        maxHeight = h;
                    }
                }
            }

            if (maxHeight - minHeight > maxSlopeVoxels)
            {
                plantHeight = 0;
                return false;
            }

            plantHeight = minHeight;
            return true;
        }

        private static bool TryFindSurfaceInChunk(
            VoxelWorld world,
            int worldX,
            int worldZ,
            int chunkOriginY,
            int chunkSize,
            VoxelType surfaceType,
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

                if (voxel.Type == surfaceType)
                {
                    surfaceHeight = worldY;
                    return true;
                }

                break;
            }

            surfaceHeight = 0;
            return false;
        }

        private bool PassesDensityGate(int worldX, int worldZ, float density, int salt)
        {
            if (worldX * worldX + worldZ * worldZ < _spawnExclusionRadius * _spawnExclusionRadius)
            {
                return false;
            }

            return HashToUnit(worldX, worldZ, salt) < density;
        }

        private bool IsSpacingOk(int worldX, int worldZ, int minSpacing)
        {
            int minSpacingSq = minSpacing * minSpacing;

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

        private void SpawnProp(
            GameObject root,
            int worldX,
            int surfaceHeight,
            int worldZ,
            PropFamily family,
            PropMeshLibrary.PropVariant variant
        )
        {
            float rotationY = HashToUnit(worldX, worldZ, family.DensitySalt + 2) * 360f;

            var propObject = new GameObject(
                $"{family.Biome}Prop ({worldX}, {surfaceHeight}, {worldZ})"
            );
            propObject.transform.SetParent(root.transform, false);
            propObject.transform.localPosition =
                new Vector3(worldX + 0.5f, surfaceHeight + 1, worldZ + 0.5f) * _config.VoxelSize;
            propObject.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

            propObject.AddComponent<MeshFilter>().sharedMesh = variant.Mesh;
            propObject.AddComponent<MeshRenderer>().sharedMaterial = propMaterial;

            // Hors du bake NavMesh (voir NavMeshRegionBaker.layerMask) : le joueur
            // collisionne toujours avec, mais le sommet du tronc n'est plus marchable.
            int vegetationLayer = LayerMask.NameToLayer("Vegetation");
            if (vegetationLayer >= 0)
            {
                propObject.layer = vegetationLayer;
            }

            if (variant.HasCollider)
            {
                BoxCollider collider = propObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(
                    variant.ColliderWidth,
                    variant.ColliderHeight,
                    variant.ColliderWidth
                );
                collider.center = new Vector3(0f, variant.ColliderHeight * 0.5f, 0f);
            }
        }

        private static Material CreatePropMaterial()
        {
            Shader shader = Shader.Find("CubeWorld/VoxelTerrain");
            return shader != null ? new Material(shader) : null;
        }

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
