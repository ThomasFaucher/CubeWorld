using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Système de nuages, indépendant du streaming de chunks : un pool fixe de
    /// nuages en mini-cubes qui dérivent au vent et se wrappent autour de la
    /// cible de vue (voir <see cref="CloudField"/>). Lit
    /// <see cref="WorldBootstrap.ViewTarget"/> directement, comme
    /// <see cref="NavMeshRegionBaker"/> : World ne doit jamais dépendre de Player.
    /// </summary>
    public sealed class CloudSystem : MonoBehaviour
    {
        // Au-delà de ce ratio du demi-côté de wrap, le renderer est coupé :
        // le téléport toroïdal a lieu hors écran.
        private const float VisibleRadiusFactor = 0.78f;

        [Header("Références")]
        [SerializeField]
        private WorldBootstrap _worldBootstrap;

        [SerializeField]
        private CloudConfig _config;

        private CloudConfig config;
        private Material cloudMaterial;
        private Mesh[] cloudVariants;
        private Transform[] cloudTransforms;
        private MeshFilter[] cloudFilters;
        private MeshRenderer[] cloudRenderers;
        private CloudField field;
        private Random rng;
        private float visibleRadiusSq;

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<CloudConfig>();
            cloudMaterial = CreateCloudMaterial();
            cloudVariants = BuildCloudVariants(config);

            float visibleRadius = config.RecycleRadius * VisibleRadiusFactor;
            visibleRadiusSq = visibleRadius * visibleRadius;

            // Graine fixe : les nuages sont purement décoratifs, pas besoin de
            // les faire dépendre de WorldConfig.Seed (contrairement au terrain).
            rng = new Random(12345u);
            field = new CloudField(config, GetViewCenterXZ(), ref rng);

            CreatePool();
        }

        private void Update()
        {
            if (field == null)
            {
                return;
            }

            Vector2 windVelocity = config.WindDirection.normalized * config.WindSpeed;
            Vector2 viewCenterXZ = GetViewCenterXZ();
            field.Advance(Time.deltaTime, windVelocity, viewCenterXZ, config);

            SyncPool(viewCenterXZ);
        }

        private Vector2 GetViewCenterXZ()
        {
            Transform target = _worldBootstrap != null ? _worldBootstrap.ViewTarget : null;
            return target != null
                ? new Vector2(target.position.x, target.position.z)
                : Vector2.zero;
        }

        private void CreatePool()
        {
            CloudField.CloudInstance[] clouds = field.Clouds;
            cloudTransforms = new Transform[clouds.Length];
            cloudFilters = new MeshFilter[clouds.Length];
            cloudRenderers = new MeshRenderer[clouds.Length];

            Vector2 viewCenterXZ = GetViewCenterXZ();

            for (int i = 0; i < clouds.Length; i++)
            {
                var cloudObject = new GameObject($"Cloud {i}");
                cloudObject.transform.SetParent(transform, false);
                cloudObject.transform.position = clouds[i].Position;
                cloudObject.transform.localScale = Vector3.one * clouds[i].Scale;

                var filter = cloudObject.AddComponent<MeshFilter>();
                filter.sharedMesh = VariantFor(clouds[i]);
                var renderer = cloudObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cloudMaterial;
                renderer.enabled = IsVisible(clouds[i].Position, viewCenterXZ);

                cloudTransforms[i] = cloudObject.transform;
                cloudFilters[i] = filter;
                cloudRenderers[i] = renderer;
            }
        }

        private void SyncPool(Vector2 viewCenterXZ)
        {
            CloudField.CloudInstance[] clouds = field.Clouds;

            for (int i = 0; i < clouds.Length; i++)
            {
                cloudTransforms[i].position = clouds[i].Position;
                cloudTransforms[i].localScale = Vector3.one * clouds[i].Scale;
                cloudRenderers[i].enabled = IsVisible(clouds[i].Position, viewCenterXZ);

                Mesh variant = VariantFor(clouds[i]);
                if (cloudFilters[i].sharedMesh != variant)
                {
                    cloudFilters[i].sharedMesh = variant;
                }
            }
        }

        private bool IsVisible(Vector3 position, Vector2 viewCenterXZ)
        {
            float dx = position.x - viewCenterXZ.x;
            float dz = position.z - viewCenterXZ.y;
            return (dx * dx) + (dz * dz) <= visibleRadiusSq;
        }

        private Mesh VariantFor(CloudField.CloudInstance cloud)
        {
            return cloudVariants[cloud.VariantIndex % cloudVariants.Length];
        }

        // Variantes construites une seule fois, partagées par tout le pool —
        // même logique que TreeMeshLibrary.
        private static Mesh[] BuildCloudVariants(CloudConfig config)
        {
            var meshes = new Mesh[Mathf.Max(1, config.CloudVariantCount)];
            var rng = new Random(1u);

            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i] = BuildCloudVariant(config, ref rng, i);
            }

            return meshes;
        }

        // Plusieurs blobs sphériques décalés : formes plus organiques que
        // une seule sphère aplatie.
        private static Mesh BuildCloudVariant(CloudConfig config, ref Random rng, int variantIndex)
        {
            var mesh = new VoxelCubeMeshData();
            var color = new Color32(240, 240, 245, 255);

            int blobCount = rng.NextInt(2, 6);
            for (int b = 0; b < blobCount; b++)
            {
                int sizeX = rng.NextInt(4, 11);
                int sizeZ = rng.NextInt(4, 11);
                int sizeY = Mathf.Max(2, rng.NextInt(2, Mathf.Max(3, sizeX / 2)));

                int offsetX = rng.NextInt(-5, 6);
                int offsetY = rng.NextInt(-1, 2);
                int offsetZ = rng.NextInt(-5, 6);

                VoxelCubeMeshBuilder.AddPart(
                    mesh,
                    new Vector3Int(offsetX - (sizeX / 2), offsetY - (sizeY / 2), offsetZ - (sizeZ / 2)),
                    sizeX,
                    sizeY,
                    sizeZ,
                    VoxelCubeShape.Sphere,
                    color,
                    config.CloudUnit,
                    (int)rng.NextUInt()
                );
            }

            return mesh.ToMesh($"CloudVariant_{variantIndex}");
        }

        private static Material CreateCloudMaterial()
        {
            Shader shader = Shader.Find("CubeWorld/VoxelTerrain");
            return shader != null ? new Material(shader) : null;
        }
    }
}
