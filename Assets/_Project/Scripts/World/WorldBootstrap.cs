using System.Collections.Generic;
using CubeWorld.Core;
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

        [Tooltip("Configuration de la végétation (touffes d'herbe...). Si vide, des valeurs par défaut sont utilisées.")]
        [SerializeField] private VegetationConfig _vegetationConfig;

        [Tooltip("Le monde se génère autour de cette cible. Si vide : la caméra principale.")]
        [SerializeField] private Transform _viewTarget;

        [Header("Streaming")]
        [Tooltip("Colonnes de chunks générées par frame. Plus haut = remplissage plus rapide mais frames plus lourdes.")]
        [SerializeField] private int _columnsPerFrame = 2;

        [Tooltip("Meshes de chunks matérialisés par frame (les plus proches d'abord). Lisse le coût de création des meshes sur plusieurs frames au lieu d'un pic.")]
        [SerializeField] private int _meshesPerFrame = 4;

        [Tooltip("Place la cible au-dessus du terrain au démarrage.")]
        [SerializeField] private bool _placeTargetAboveTerrain = true;

        private WorldConfig config;
        private Material terrainMaterial;
        private Material waterMaterial;
        private VegetationConfig vegetationConfig;
        private VoxelWorld world;
        private ChunkStreamer streamer;
        private readonly Dictionary<int3, ChunkVisual> chunkVisuals = new();

        // Jobs de meshing planifiés mais pas encore terminés (voir OnChunkMeshDirty).
        private readonly Dictionary<int3, PendingMesh> pendingMeshes = new();
        private readonly List<int3> completedMeshBuffer = new();

        // Cuissons de MeshCollider en cours sur les threads de fond (voir MaterializeChunk).
        private readonly Dictionary<int3, PendingColliderBake> pendingColliderBakes = new();
        private readonly List<int3> completedBakeBuffer = new();

        // Bornes locales d'un mesh de chunk, connues d'avance (0..ChunkSize, avec une
        // marge verticale pour les brins d'herbe qui dépassent du voxel du dessus) :
        // évite le RecalculateBounds par mesh sur le thread principal.
        private Bounds chunkLocalBounds;

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<WorldConfig>();
            terrainMaterial = _terrainMaterial != null ? _terrainMaterial : CreateDefaultMaterial("CubeWorld/VoxelTerrain");
            waterMaterial = _waterMaterial != null ? _waterMaterial : CreateDefaultMaterial("CubeWorld/VoxelWater");
            vegetationConfig = _vegetationConfig != null ? _vegetationConfig : ScriptableObject.CreateInstance<VegetationConfig>();
            world = new VoxelWorld(config);
            streamer = new ChunkStreamer(world, config);

            float half = config.ChunkSize * 0.5f;
            chunkLocalBounds = new Bounds(
                new Vector3(half, half + 1f, half),
                new Vector3(config.ChunkSize, config.ChunkSize + 4f, config.ChunkSize));
        }

        private void Start()
        {
            if (_placeTargetAboveTerrain && ViewTarget is { } target)
            {
                int surfaceHeight = world.GetSurfaceHeight(0, 0);
                target.position = new Vector3(0f, surfaceHeight * config.VoxelSize + 20f, 0f);
            }
        }

        private void Update()
        {
            CompleteColliderBakes();
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
                pending.Foliage.Dispose();
            }

            pendingMeshes.Clear();

            foreach (PendingColliderBake bake in pendingColliderBakes.Values)
            {
                bake.Handle.Complete();
            }

            pendingColliderBakes.Clear();

            world?.Dispose();
        }

        /// <summary>Le monde métier (voxels, biomes...). Public pour VegetationSpawner.</summary>
        public VoxelWorld World => world;

        /// <summary>Cible autour de laquelle le monde streame (voir SetViewTarget). Public pour NavMeshRegionBaker.</summary>
        public Transform ViewTarget
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

        /// <summary>
        /// Change la cible autour de laquelle le monde streame (et, si
        /// <see cref="_placeTargetAboveTerrain"/> est actif, sera placée
        /// au-dessus du terrain dans <see cref="Start"/>). Destiné à être
        /// appelé depuis <c>Awake()</c> d'un autre script (ex. PlayerBootstrap) :
        /// Unity garantit que tous les Awake s'exécutent avant tous les Start,
        /// donc la cible est déjà en place quand ce composant se lance.
        /// </summary>
        public void SetViewTarget(Transform target)
        {
            _viewTarget = target;
        }

        // Colonne de chunks (x, z) contenant cette position monde. La largeur
        // d'une colonne, en unités monde, est ChunkSize (voxels) * VoxelSize.
        private int2 WorldToColumn(Vector3 position)
        {
            float columnWorldSize = config.ChunkSize * config.VoxelSize;
            return new int2(
                (int)math.floor(position.x / columnWorldSize),
                (int)math.floor(position.z / columnWorldSize));
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
                stale.Foliage.Dispose();
            }

            var opaqueData = new ChunkMeshData(Allocator.Persistent);
            var waterData = new ChunkMeshData(Allocator.Persistent);
            var foliageData = new ChunkMeshData(Allocator.Persistent);
            JobHandle handle = ChunkMeshBuilder.ScheduleBuild(chunk, world, opaqueData, waterData, foliageData, vegetationConfig);

            world.SetMeshHandle(coord, handle);
            pendingMeshes[coord] = new PendingMesh(chunk, opaqueData, waterData, foliageData, handle);
        }

        // À appeler chaque frame, avant tout traitement pouvant décharger des
        // chunks : matérialise les jobs de meshing terminés — au plus
        // _meshesPerFrame par frame, les plus proches de la cible d'abord (le
        // sol sous le joueur avant l'horizon), le reste attend les frames
        // suivantes. Lisse le coût de création des meshes au lieu d'un pic
        // quand beaucoup de jobs se terminent en même temps.
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

            if (completedMeshBuffer.Count == 0)
            {
                return;
            }

            Vector3 targetPosition = ViewTarget is { } target ? target.position : Vector3.zero;
            float chunkWorldSize = config.ChunkSize * config.VoxelSize;
            completedMeshBuffer.Sort((a, b) =>
                ChunkDistanceSq(a, targetPosition, chunkWorldSize)
                    .CompareTo(ChunkDistanceSq(b, targetPosition, chunkWorldSize)));

            int budget = Mathf.Max(1, _meshesPerFrame);
            for (int i = 0; i < completedMeshBuffer.Count && i < budget; i++)
            {
                int3 coord = completedMeshBuffer[i];
                PendingMesh pending = pendingMeshes[coord];
                pendingMeshes.Remove(coord);
                pending.Handle.Complete();

                MaterializeChunk(pending.Chunk, pending.Opaque, pending.Water, pending.Foliage);

                pending.Opaque.Dispose();
                pending.Water.Dispose();
                pending.Foliage.Dispose();
                world.ClearMeshHandle(coord);
            }
        }

        private static float ChunkDistanceSq(int3 coord, Vector3 targetPosition, float chunkWorldSize)
        {
            var center = new Vector3(coord.x + 0.5f, coord.y + 0.5f, coord.z + 0.5f) * chunkWorldSize;
            return (center - targetPosition).sqrMagnitude;
        }

        // Assigne aux colliders les meshes dont la cuisson de fond est terminée :
        // l'assignation réutilise alors les données cuites (quasi gratuite), au
        // lieu de cuire en synchrone sur le thread principal.
        private void CompleteColliderBakes()
        {
            if (pendingColliderBakes.Count == 0)
            {
                return;
            }

            completedBakeBuffer.Clear();
            foreach (KeyValuePair<int3, PendingColliderBake> pair in pendingColliderBakes)
            {
                if (pair.Value.Handle.IsCompleted)
                {
                    completedBakeBuffer.Add(pair.Key);
                }
            }

            foreach (int3 coord in completedBakeBuffer)
            {
                PendingColliderBake bake = pendingColliderBakes[coord];
                pendingColliderBakes.Remove(coord);
                bake.Handle.Complete();

                // Le visual a pu être détruit entre-temps (déchargement pendant la
                // cuisson) : dans ce cas le mesh a déjà été détruit avec lui.
                if (bake.Collider != null)
                {
                    bake.Collider.sharedMesh = bake.Mesh;
                }

                if (bake.PreviousMesh != null)
                {
                    Destroy(bake.PreviousMesh);
                }
            }
        }

        private void MaterializeChunk(Chunk chunk, ChunkMeshData opaqueData, ChunkMeshData waterData, ChunkMeshData foliageData)
        {
            bool hasVisual = chunkVisuals.TryGetValue(chunk.Coord, out ChunkVisual visual);

            // Un bake de collider peut être en cours pour ce chunk (remesh rapproché,
            // ou chunk devenu vide) : son résultat est obsolète, on le termine et on
            // libère l'ancien mesh qu'il retenait.
            if (pendingColliderBakes.Remove(chunk.Coord, out PendingColliderBake staleBake))
            {
                staleBake.Handle.Complete();
                if (staleBake.PreviousMesh != null)
                {
                    Destroy(staleBake.PreviousMesh);
                }
            }

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

            // Le rendu est mis à jour immédiatement ; la collision (même mesh) suit
            // un ou deux frames plus tard, une fois la cuisson terminée en fond —
            // voir le bake plus bas. L'ancien mesh reste vivant (et assigné au
            // collider) jusque-là : jamais de trou de collision pendant un remesh.
            Mesh opaqueMesh = opaqueData.IsEmpty ? null : opaqueData.ToMesh(chunkLocalBounds);
            Mesh previousOpaqueMesh = visual.OpaqueFilter.sharedMesh;
            visual.OpaqueFilter.sharedMesh = opaqueMesh;

            if (opaqueMesh == null)
            {
                visual.OpaqueCollider.sharedMesh = null;
                if (previousOpaqueMesh != null)
                {
                    Destroy(previousOpaqueMesh);
                }
            }
            else
            {
                // Cuisson du MeshCollider hors du thread principal (Physics.BakeMesh
                // est thread-safe) : l'assignation du collider, dans
                // CompleteColliderBakes, réutilisera les données déjà cuites au lieu
                // de cuire en bloquant la frame — c'était le principal coût de
                // matérialisation d'un chunk. L'eau n'a pas de collider (pas solide,
                // voir Voxel.IsSolid).
                JobHandle bakeHandle = new ColliderBakeJob { MeshId = opaqueMesh.GetEntityId() }.Schedule();
                pendingColliderBakes[chunk.Coord] =
                    new PendingColliderBake(visual.OpaqueCollider, opaqueMesh, previousOpaqueMesh, bakeHandle);
            }

            bool hasWater = !waterData.IsEmpty;
            visual.WaterObject.SetActive(hasWater);
            ApplyMesh(visual.WaterFilter, hasWater ? waterData.ToMesh(chunkLocalBounds) : null);

            // Les touffes n'ont pas de collider (voir CreateChunkVisual) : le
            // joueur ne doit jamais trébucher sur de l'herbe décorative.
            bool hasFoliage = !foliageData.IsEmpty;
            visual.FoliageObject.SetActive(hasFoliage);
            ApplyMesh(visual.FoliageFilter, hasFoliage ? foliageData.ToMesh(chunkLocalBounds) : null);

            // Signale qu'un collider de terrain vient de (re)paraître : NavMeshRegionBaker
            // s'en sert pour savoir quand rebaker.
            EventBus.Publish(new ChunkMeshMaterializedEvent(
                new Vector3Int(chunk.Coord.x, chunk.Coord.y, chunk.Coord.z),
                (Vector3)(float3)chunk.WorldOrigin * config.VoxelSize));
        }

        private ChunkVisual CreateChunkVisual(Chunk chunk)
        {
            var root = new GameObject($"Chunk ({chunk.Coord.x}, {chunk.Coord.y}, {chunk.Coord.z})");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = (float3)chunk.WorldOrigin * config.VoxelSize;
            // Le mesh est généré en unités de voxel (1 = un cube) ; l'échelle du
            // transform le ramène à la vraie taille monde (voir WorldConfig.VoxelSize).
            // S'applique aussi au collider (MeshCollider suit l'échelle du transform).
            root.transform.localScale = Vector3.one * config.VoxelSize;

            var opaqueFilter = root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            var opaqueCollider = root.AddComponent<MeshCollider>();

            var waterObject = new GameObject("Water");
            waterObject.transform.SetParent(root.transform, false);
            var waterFilter = waterObject.AddComponent<MeshFilter>();
            waterObject.AddComponent<MeshRenderer>().sharedMaterial = waterMaterial;
            waterObject.SetActive(false);

            // Pas de MeshCollider : les touffes d'herbe sont purement décoratives,
            // le joueur ne doit jamais buter dessus.
            var foliageObject = new GameObject("Foliage");
            foliageObject.transform.SetParent(root.transform, false);
            var foliageFilter = foliageObject.AddComponent<MeshFilter>();
            foliageObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            foliageObject.SetActive(false);

            return new ChunkVisual(root, opaqueFilter, opaqueCollider, waterObject, waterFilter, foliageObject, foliageFilter);
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
                pending.Foliage.Dispose();
            }

            // Un bake de collider encore en vol pour ce chunk devient inutile : on
            // le termine et on libère l'ancien mesh qu'il retenait (le nouveau est
            // détruit juste en dessous, via le MeshFilter du visual).
            if (pendingColliderBakes.Remove(coord, out PendingColliderBake bake))
            {
                bake.Handle.Complete();
                if (bake.PreviousMesh != null)
                {
                    Destroy(bake.PreviousMesh);
                }
            }

            if (chunkVisuals.Remove(coord, out ChunkVisual visual))
            {
                DestroyChunkVisual(visual);
            }

            EventBus.Publish(new ChunkUnloadedEvent(new Vector3Int(coord.x, coord.y, coord.z)));
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

            Mesh foliageMesh = visual.FoliageFilter.sharedMesh;
            if (foliageMesh != null)
            {
                Destroy(foliageMesh);
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
        // opaque, avec un enfant dédié à l'eau (matériau transparent séparé) et un
        // enfant dédié aux touffes d'herbe (mesh à part, sans collider).
        private readonly struct ChunkVisual
        {
            public readonly GameObject Root;
            public readonly MeshFilter OpaqueFilter;
            public readonly MeshCollider OpaqueCollider;
            public readonly GameObject WaterObject;
            public readonly MeshFilter WaterFilter;
            public readonly GameObject FoliageObject;
            public readonly MeshFilter FoliageFilter;

            public ChunkVisual(
                GameObject root,
                MeshFilter opaqueFilter,
                MeshCollider opaqueCollider,
                GameObject waterObject,
                MeshFilter waterFilter,
                GameObject foliageObject,
                MeshFilter foliageFilter)
            {
                Root = root;
                OpaqueFilter = opaqueFilter;
                OpaqueCollider = opaqueCollider;
                WaterObject = waterObject;
                WaterFilter = waterFilter;
                FoliageObject = foliageObject;
                FoliageFilter = foliageFilter;
            }
        }

        private readonly struct PendingMesh
        {
            public readonly Chunk Chunk;
            public readonly ChunkMeshData Opaque;
            public readonly ChunkMeshData Water;
            public readonly ChunkMeshData Foliage;
            public readonly JobHandle Handle;

            public PendingMesh(Chunk chunk, ChunkMeshData opaque, ChunkMeshData water, ChunkMeshData foliage, JobHandle handle)
            {
                Chunk = chunk;
                Opaque = opaque;
                Water = water;
                Foliage = foliage;
                Handle = handle;
            }
        }

        // Cuisson d'un mesh de collision sur un thread de fond. Pas de Burst :
        // Physics.BakeMesh est un appel moteur (thread-safe), pas du code compilable.
        private struct ColliderBakeJob : IJob
        {
            public EntityId MeshId;

            public void Execute()
            {
                Physics.BakeMesh(MeshId, false);
            }
        }

        // Mesh cuit en fond, en attente d'assignation à son collider. PreviousMesh
        // est l'ancien mesh, gardé vivant (et toujours assigné au collider) jusqu'à
        // la fin de la cuisson pour ne jamais laisser un trou de collision.
        private readonly struct PendingColliderBake
        {
            public readonly MeshCollider Collider;
            public readonly Mesh Mesh;
            public readonly Mesh PreviousMesh;
            public readonly JobHandle Handle;

            public PendingColliderBake(MeshCollider collider, Mesh mesh, Mesh previousMesh, JobHandle handle)
            {
                Collider = collider;
                Mesh = mesh;
                PreviousMesh = previousMesh;
                Handle = handle;
            }
        }
    }
}
