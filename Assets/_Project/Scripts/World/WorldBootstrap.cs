using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Pilote le monde côté scène : chaque frame, signale au streamer la position
    /// de la cible (caméra ou joueur), planifie les jobs de meshing des chunks
    /// dont le voisinage a changé, et matérialise en GameObjects ceux dont le job
    /// vient de se terminer. Seul point de contact entre la scène et le métier.
    /// </summary>
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Configuration du monde. Si vide, des valeurs par défaut sont utilisées.")]
        [SerializeField] private WorldConfig _config;

        [Tooltip("Matériau du terrain opaque. Si vide, un matériau CubeWorld/VoxelTerrain est créé.")]
        [SerializeField] private Material _terrainMaterial;

        [Tooltip("Matériau de l'eau (transparent). Si vide, un matériau CubeWorld/VoxelWater est créé.")]
        [SerializeField] private Material _waterMaterial;

        [Tooltip("Le monde se génère autour de cette cible. Si vide : la caméra principale.")]
        [SerializeField] private Transform _viewTarget;

        [Header("Streaming")]
        [Tooltip("Colonnes de chunks générées par frame. Plus haut = remplissage plus rapide mais frames plus lourdes.")]
        [SerializeField] private int _columnsPerFrame = 2;

        [Tooltip("Place la cible au-dessus du terrain au démarrage.")]
        [SerializeField] private bool _placeTargetAboveTerrain = true;

        private WorldConfig config;
        private Material terrainMaterial;
        private Material waterMaterial;
        private VoxelWorld world;
        private ChunkStreamer streamer;
        private readonly Dictionary<int3, ChunkVisual> chunkVisuals = new();

        // Jobs de meshing planifiés mais pas encore terminés (voir OnChunkMeshDirty).
        private readonly Dictionary<int3, PendingMesh> pendingMeshes = new();
        private readonly List<int3> completedMeshBuffer = new();

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<WorldConfig>();
            terrainMaterial = _terrainMaterial != null ? _terrainMaterial : CreateDefaultMaterial("CubeWorld/VoxelTerrain");
            waterMaterial = _waterMaterial != null ? _waterMaterial : CreateDefaultMaterial("CubeWorld/VoxelWater");
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
            CompletePendingMeshes();

            if (ViewTarget is not { } target)
            {
                return;
            }

            streamer.SetCenter(WorldToColumn(target.position));
            streamer.Process(_columnsPerFrame, OnChunkMeshDirty, OnChunkUnloaded);
        }

        private void OnDestroy()
        {
            // Termine et libère tout job/allocation native en attente : évite les
            // fuites et les exceptions de sécurité au changement de scène.
            foreach (PendingMesh pending in pendingMeshes.Values)
            {
                pending.Handle.Complete();
                pending.Opaque.Dispose();
                pending.Water.Dispose();
            }

            pendingMeshes.Clear();

            world?.Dispose();
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

        // Appelé pour tout chunk dont le mesh doit être (re)construit : premier
        // chargement, ou voisinage changé (voir ChunkStreamer). Ne bloque pas :
        // planifie un job Burst, matérialisé plus tard par CompletePendingMeshes.
        private void OnChunkMeshDirty(Chunk chunk)
        {
            int3 coord = chunk.Coord;

            // Un remesh peut être redemandé avant que le précédent job n'ait fini
            // (ex. deux voisins chargés à des frames rapprochées) : on le termine
            // et on jette son résultat, devenu obsolète, avant d'en replanifier un.
            if (pendingMeshes.Remove(coord, out PendingMesh stale))
            {
                stale.Handle.Complete();
                stale.Opaque.Dispose();
                stale.Water.Dispose();
            }

            var opaqueData = new ChunkMeshData(Allocator.Persistent);
            var waterData = new ChunkMeshData(Allocator.Persistent);
            JobHandle handle = ChunkMeshBuilder.ScheduleBuild(chunk, world, opaqueData, waterData);

            world.SetMeshHandle(coord, handle);
            pendingMeshes[coord] = new PendingMesh(chunk, opaqueData, waterData, handle);
        }

        // À appeler chaque frame, avant tout traitement pouvant décharger des
        // chunks : matérialise les jobs de meshing terminés, laisse les autres
        // en attente pour une frame suivante.
        private void CompletePendingMeshes()
        {
            if (pendingMeshes.Count == 0)
            {
                return;
            }

            completedMeshBuffer.Clear();
            foreach (KeyValuePair<int3, PendingMesh> pair in pendingMeshes)
            {
                if (pair.Value.Handle.IsCompleted)
                {
                    completedMeshBuffer.Add(pair.Key);
                }
            }

            foreach (int3 coord in completedMeshBuffer)
            {
                PendingMesh pending = pendingMeshes[coord];
                pendingMeshes.Remove(coord);
                pending.Handle.Complete();

                MaterializeChunk(pending.Chunk, pending.Opaque, pending.Water);

                pending.Opaque.Dispose();
                pending.Water.Dispose();
                world.ClearMeshHandle(coord);
            }
        }

        private void MaterializeChunk(Chunk chunk, ChunkMeshData opaqueData, ChunkMeshData waterData)
        {
            bool hasVisual = chunkVisuals.TryGetValue(chunk.Coord, out ChunkVisual visual);

            if (opaqueData.IsEmpty && waterData.IsEmpty)
            {
                if (hasVisual)
                {
                    DestroyChunkVisual(visual);
                    chunkVisuals.Remove(chunk.Coord);
                }

                return;
            }

            if (!hasVisual)
            {
                visual = CreateChunkVisual(chunk);
                chunkVisuals[chunk.Coord] = visual;
            }

            ApplyMesh(visual.OpaqueFilter, opaqueData.IsEmpty ? null : opaqueData.ToMesh());

            bool hasWater = !waterData.IsEmpty;
            visual.WaterObject.SetActive(hasWater);
            ApplyMesh(visual.WaterFilter, hasWater ? waterData.ToMesh() : null);
        }

        private ChunkVisual CreateChunkVisual(Chunk chunk)
        {
            var root = new GameObject($"Chunk ({chunk.Coord.x}, {chunk.Coord.y}, {chunk.Coord.z})");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = (float3)chunk.WorldOrigin;

            var opaqueFilter = root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;

            var waterObject = new GameObject("Water");
            waterObject.transform.SetParent(root.transform, false);
            var waterFilter = waterObject.AddComponent<MeshFilter>();
            waterObject.AddComponent<MeshRenderer>().sharedMaterial = waterMaterial;
            waterObject.SetActive(false);

            return new ChunkVisual(root, opaqueFilter, waterObject, waterFilter);
        }

        // Remplace le mesh d'un MeshFilter, en détruisant l'ancien (asset runtime).
        // Un mesh null vide simplement le filtre (rien à afficher sur cette moitié).
        private void ApplyMesh(MeshFilter filter, Mesh mesh)
        {
            Mesh oldMesh = filter.sharedMesh;
            filter.sharedMesh = mesh;

            if (oldMesh != null)
            {
                Destroy(oldMesh);
            }
        }

        private void OnChunkUnloaded(int3 coord)
        {
            // Un job de meshing pour ce chunk a pu être planifié juste avant son
            // déchargement (VoxelWorld.RemoveChunk l'a déjà forcé à se terminer) :
            // son résultat est désormais inutile, on le jette sans matérialiser.
            if (pendingMeshes.Remove(coord, out PendingMesh pending))
            {
                pending.Handle.Complete();
                pending.Opaque.Dispose();
                pending.Water.Dispose();
            }

            if (chunkVisuals.Remove(coord, out ChunkVisual visual))
            {
                DestroyChunkVisual(visual);
            }
        }

        private void DestroyChunkVisual(ChunkVisual visual)
        {
            // Les meshes sont des assets runtime : les détruire explicitement, sinon ils fuient.
            Mesh opaqueMesh = visual.OpaqueFilter.sharedMesh;
            if (opaqueMesh != null)
            {
                Destroy(opaqueMesh);
            }

            Mesh waterMesh = visual.WaterFilter.sharedMesh;
            if (waterMesh != null)
            {
                Destroy(waterMesh);
            }

            Destroy(visual.Root);
        }

        private static Material CreateDefaultMaterial(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[CubeWorld] Shader « {shaderName} » introuvable.");
                return null;
            }

            return new Material(shader);
        }

        // Représentation scène d'un chunk : un GameObject racine pour le terrain
        // opaque, avec un enfant dédié à l'eau (matériau transparent séparé).
        private readonly struct ChunkVisual
        {
            public readonly GameObject Root;
            public readonly MeshFilter OpaqueFilter;
            public readonly GameObject WaterObject;
            public readonly MeshFilter WaterFilter;

            public ChunkVisual(GameObject root, MeshFilter opaqueFilter, GameObject waterObject, MeshFilter waterFilter)
            {
                Root = root;
                OpaqueFilter = opaqueFilter;
                WaterObject = waterObject;
                WaterFilter = waterFilter;
            }
        }

        private readonly struct PendingMesh
        {
            public readonly Chunk Chunk;
            public readonly ChunkMeshData Opaque;
            public readonly ChunkMeshData Water;
            public readonly JobHandle Handle;

            public PendingMesh(Chunk chunk, ChunkMeshData opaque, ChunkMeshData water, JobHandle handle)
            {
                Chunk = chunk;
                Opaque = opaque;
                Water = water;
                Handle = handle;
            }
        }
    }
}
