using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Pilote le monde côté scène : chaque frame, signale au streamer la position
    /// de la cible (caméra ou joueur) et matérialise les chunks chargés/déchargés
    /// en GameObjects. Seul point de contact entre la scène et le métier.
    /// </summary>
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Configuration du monde. Si vide, des valeurs par défaut sont utilisées.")]
        [SerializeField] private WorldConfig _config;

        [Tooltip("Matériau du terrain. Si vide, un matériau CubeWorld/VoxelTerrain est créé.")]
        [SerializeField] private Material _terrainMaterial;

        [Tooltip("Le monde se génère autour de cette cible. Si vide : la caméra principale.")]
        [SerializeField] private Transform _viewTarget;

        [Header("Streaming")]
        [Tooltip("Colonnes de chunks générées par frame. Plus haut = remplissage plus rapide mais frames plus lourdes.")]
        [SerializeField] private int _columnsPerFrame = 2;

        [Tooltip("Place la cible au-dessus du terrain au démarrage.")]
        [SerializeField] private bool _placeTargetAboveTerrain = true;

        private WorldConfig config;
        private Material material;
        private VoxelWorld world;
        private ChunkStreamer streamer;
        private readonly Dictionary<int3, GameObject> chunkObjects = new();

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<WorldConfig>();
            material = _terrainMaterial != null ? _terrainMaterial : CreateDefaultMaterial();
            world = new VoxelWorld(config);
            streamer = new ChunkStreamer(world, config);
        }

        private void Start()
        {
            if (_placeTargetAboveTerrain && ViewTarget is { } target)
            {
                int surfaceHeight = world.GetSurfaceHeight(0, 0);
                target.position = new Vector3(0f, surfaceHeight + 20f, 0f);
            }
        }

        private void Update()
        {
            if (ViewTarget is not { } target)
            {
                return;
            }

            streamer.SetCenter(WorldToColumn(target.position));
            streamer.Process(_columnsPerFrame, OnChunkLoaded, OnChunkUnloaded);
        }

        private Transform ViewTarget
        {
            get
            {
                if (_viewTarget != null)
                {
                    return _viewTarget;
                }

                Camera camera = Camera.main;
                return camera != null ? camera.transform : null;
            }
        }

        // Colonne de chunks (x, z) contenant cette position monde.
        private int2 WorldToColumn(Vector3 position)
        {
            return new int2(
                (int)math.floor(position.x / config.ChunkSize),
                (int)math.floor(position.z / config.ChunkSize));
        }

        private void OnChunkLoaded(Chunk chunk)
        {
            ChunkMeshData meshData = ChunkMeshBuilder.Build(chunk, world);
            if (meshData.IsEmpty)
            {
                return;
            }

            var chunkObject = new GameObject($"Chunk ({chunk.Coord.x}, {chunk.Coord.y}, {chunk.Coord.z})");
            chunkObject.transform.SetParent(transform, false);
            chunkObject.transform.localPosition = (float3)chunk.WorldOrigin;

            chunkObject.AddComponent<MeshFilter>().sharedMesh = meshData.ToMesh();
            chunkObject.AddComponent<MeshRenderer>().sharedMaterial = material;

            chunkObjects[chunk.Coord] = chunkObject;
        }

        private void OnChunkUnloaded(int3 coord)
        {
            if (!chunkObjects.Remove(coord, out GameObject chunkObject))
            {
                return;
            }

            // Le mesh est un asset runtime : le détruire explicitement, sinon il fuit.
            Destroy(chunkObject.GetComponent<MeshFilter>().sharedMesh);
            Destroy(chunkObject);
        }

        private static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("CubeWorld/VoxelTerrain");
            if (shader == null)
            {
                Debug.LogError("[CubeWorld] Shader « CubeWorld/VoxelTerrain » introuvable.");
                return null;
            }

            return new Material(shader);
        }
    }
}
