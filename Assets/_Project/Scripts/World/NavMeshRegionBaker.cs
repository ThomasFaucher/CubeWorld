using CubeWorld.Core;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace CubeWorld.World
{
    /// <summary>
    /// Rebake un NavMeshSurface borné autour de la cible du monde (le joueur),
    /// à mesure que les chunks streament. Débouncé pour ne pas rebaker à chaque
    /// chunk : attend une accalmie dans les changements avant de baker, avec un
    /// intervalle minimum entre deux bakes. Les ennemis ne doivent exister que
    /// dans la zone ainsi bakée (voir Combat.EnemySpawner).
    /// </summary>
    public sealed class NavMeshRegionBaker : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField]
        private WorldBootstrap _worldBootstrap;

        [SerializeField]
        private WorldConfig _config;

        [Header("Debounce")]
        [Tooltip("Temps d'accalmie sans changement pertinent avant de rebaker.")]
        [SerializeField]
        private float _debounceSeconds = 1f;

        [Tooltip(
            "Intervalle minimum entre deux bakes, même si des changements continuent d'arriver."
        )]
        [SerializeField]
        private float _minRebakeInterval = 2f;

        private NavMeshSurface surface;
        private bool dirty;
        private bool hasBaked;
        private float lastRelevantChangeTime;
        private float lastBakeTime = float.NegativeInfinity;
        private Vector3 lastBakeCenter;
        private int version;

        private void Awake()
        {
            var surfaceObject = new GameObject("NavMeshRegion");
            surfaceObject.transform.SetParent(transform, false);

            surface = surfaceObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
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

        private void Update()
        {
            Transform target = _worldBootstrap != null ? _worldBootstrap.ViewTarget : null;
            if (target == null)
            {
                return;
            }

            if (!hasBaked)
            {
                MarkDirty();
            }
            else if (
                Vector3.Distance(target.position, lastBakeCenter)
                > _config.ChunkSize * _config.VoxelSize
            )
            {
                MarkDirty();
            }

            if (!dirty)
            {
                return;
            }

            bool debounceElapsed = Time.time - lastRelevantChangeTime >= _debounceSeconds;
            bool minIntervalElapsed = Time.time - lastBakeTime >= _minRebakeInterval;
            if (debounceElapsed && minIntervalElapsed)
            {
                Bake(target.position);
            }
        }

        private void OnChunkMeshMaterialized(ChunkMeshMaterializedEvent evt)
        {
            MarkDirtyIfInsideBakedBounds(evt.WorldOrigin);
        }

        private void OnChunkUnloaded(ChunkUnloadedEvent evt)
        {
            Vector3 worldOrigin =
                new Vector3(evt.ChunkCoord.x, evt.ChunkCoord.y, evt.ChunkCoord.z)
                * (_config.ChunkSize * _config.VoxelSize);
            MarkDirtyIfInsideBakedBounds(worldOrigin);
        }

        // Ignore les chunks hors de la zone déjà bakée : évite de rebaker pour
        // des chunks qui streament loin du joueur, hors de portée des ennemis.
        private void MarkDirtyIfInsideBakedBounds(Vector3 chunkWorldOrigin)
        {
            if (!hasBaked || ComputeBounds(lastBakeCenter).Contains(chunkWorldOrigin))
            {
                MarkDirty();
            }
        }

        private void MarkDirty()
        {
            if (!dirty)
            {
                lastRelevantChangeTime = Time.time;
            }

            dirty = true;
        }

        private void Bake(Vector3 center)
        {
            Bounds bounds = ComputeBounds(center);
            surface.transform.position = bounds.center;
            surface.size = bounds.size;
            surface.BuildNavMesh();

            dirty = false;
            hasBaked = true;
            lastBakeCenter = center;
            lastBakeTime = Time.time;
            version++;

            EventBus.Publish(new NavMeshBakedEvent(bounds.center, bounds.size, version));
        }

        // Volume borné, avec une marge d'un chunk sous la distance de vue, pour
        // rester strictement dans la zone streamée/collidée (voir ChunkStreamer).
        private Bounds ComputeBounds(Vector3 center)
        {
            int radius = Mathf.Max(1, _config.ViewDistance - 1);
            float horizontalSize = radius * _config.ChunkSize * _config.VoxelSize * 2f;
            float verticalSize = _config.MaxTerrainHeight * _config.VoxelSize;
            return new Bounds(center, new Vector3(horizontalSize, verticalSize, horizontalSize));
        }
    }
}
