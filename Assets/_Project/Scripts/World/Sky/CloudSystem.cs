using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Système de nuages, indépendant du streaming de chunks : un pool fixe de
    /// nuages en mini-cubes qui dérivent au vent et se recyclent en sortant du
    /// rayon de vue (voir <see cref="CloudField"/>). Lit
    /// <see cref="WorldBootstrap.ViewTarget"/> directement, comme
    /// <see cref="NavMeshRegionBaker"/> : World ne doit jamais dépendre de Player.
    /// </summary>
    public sealed class CloudSystem : MonoBehaviour
    {
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
        private CloudField field;
        private Random rng;

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<CloudConfig>();
            cloudMaterial = CreateCloudMaterial();
            cloudVariants = BuildCloudVariants(config);

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
            field.Advance(Time.deltaTime, windVelocity, GetViewCenterXZ(), config, ref rng);

            SyncPool();
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

            for (int i = 0; i < clouds.Length; i++)
            {
                var cloudObject = new GameObject($"Cloud {i}");
                cloudObject.transform.SetParent(transform, false);
                cloudObject.transform.position = clouds[i].Position;

                var filter = cloudObject.AddComponent<MeshFilter>();
                filter.sharedMesh = VariantFor(clouds[i]);
                cloudObject.AddComponent<MeshRenderer>().sharedMaterial = cloudMaterial;

                cloudTransforms[i] = cloudObject.transform;
                cloudFilters[i] = filter;
            }
        }

        private void SyncPool()
        {
            CloudField.CloudInstance[] clouds = field.Clouds;

            for (int i = 0; i < clouds.Length; i++)
            {
                cloudTransforms[i].position = clouds[i].Position;

                Mesh variant = VariantFor(clouds[i]);
                if (cloudFilters[i].sharedMesh != variant)
                {
                    cloudFilters[i].sharedMesh = variant;
                }
            }
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

        private static Mesh BuildCloudVariant(CloudConfig config, ref Random rng, int variantIndex)
        {
            var mesh = new VoxelCubeMeshData();

            int sizeX = rng.NextInt(6, 9);
            int sizeZ = rng.NextInt(6, 9);
            int sizeY = Mathf.Max(2, sizeX / 3);

            var color = new Color32(240, 240, 245, 255);

            VoxelCubeMeshBuilder.AddPart(
                mesh,
                new Vector3Int(-sizeX / 2, -sizeY / 2, -sizeZ / 2),
                sizeX,
                sizeY,
                sizeZ,
                VoxelCubeShape.Sphere,
                color,
                config.CloudUnit,
                (int)rng.NextUInt()
            );

            return mesh.ToMesh($"CloudVariant_{variantIndex}");
        }

        private static Material CreateCloudMaterial()
        {
            Shader shader = Shader.Find("CubeWorld/VoxelTerrain");
            return shader != null ? new Material(shader) : null;
        }
    }
}
