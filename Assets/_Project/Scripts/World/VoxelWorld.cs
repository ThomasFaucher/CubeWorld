using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Le monde voxel : possède les chunks, planifie leur génération (job Burst)
    /// et fournit l'accès global aux voxels par coordonnées monde. Classe métier
    /// pure ; le chargement/déchargement est orchestré par <see cref="ChunkStreamer"/>,
    /// la matérialisation en mesh par <see cref="WorldBootstrap"/>.
    /// </summary>
    public sealed class VoxelWorld : IVoxelLookup, IDisposable
    {
        // Les 6 voisins directs d'un chunk (une seule face partagée, jamais de diagonale).
        private static readonly int3[] FaceOffsets =
        {
            new(0, 0, -1), new(0, 0, 1), new(0, 1, 0), new(0, -1, 0), new(-1, 0, 0), new(1, 0, 0),
        };

        private readonly Dictionary<int3, Chunk> chunks = new();

        // Handles des jobs en cours (ou déjà terminés mais pas encore nettoyés) :
        // permet à quiconque lit un chunk d'attendre exactement ce qu'il faut,
        // et à RemoveChunk de garantir qu'aucun job n'écrit/lit encore le
        // tableau de voxels au moment de le libérer.
        private readonly Dictionary<int3, JobHandle> generationHandles = new();
        private readonly Dictionary<int3, JobHandle> meshHandles = new();

        private readonly WorldConfig config;
        private readonly TerrainGenerator generator;

        public VoxelWorld(WorldConfig config)
        {
            this.config = config;
            generator = new TerrainGenerator(config);
        }

        public IReadOnlyCollection<Chunk> Chunks => chunks.Values;

        /// <summary>
        /// Crée le chunk à cette coordonnée et planifie sa génération en tâche de
        /// fond (ou renvoie l'existant, déjà généré ou en cours de génération).
        /// </summary>
        public Chunk RequestChunk(int3 coord)
        {
            if (chunks.TryGetValue(coord, out Chunk existing))
            {
                return existing;
            }

            var chunk = new Chunk(coord, config.ChunkSize);
            chunks[coord] = chunk;
            generationHandles[coord] = generator.ScheduleGenerate(chunk);
            return chunk;
        }

        /// <summary>Handle du job de génération du chunk, ou un handle déjà terminé s'il n'y en a pas/plus.</summary>
        public JobHandle GetGenerationHandle(int3 coord)
        {
            return generationHandles.TryGetValue(coord, out JobHandle handle) ? handle : default;
        }

        /// <summary>Combine les handles de génération des voisins directs déjà chargés (les autres seront traités comme de l'air).</summary>
        public JobHandle CombineNeighborGenerationHandles(int3 coord)
        {
            JobHandle combined = default;
            foreach (int3 offset in FaceOffsets)
            {
                combined = JobHandle.CombineDependencies(combined, GetGenerationHandle(coord + offset));
            }

            return combined;
        }

        /// <summary>Voxels natifs du voisin direct dans cette direction, ou un tableau non créé s'il n'est pas chargé.</summary>
        public NativeArray<Voxel> GetNeighborVoxels(int3 coord, int3 offset)
        {
            return chunks.TryGetValue(coord + offset, out Chunk neighbor) ? neighbor.Voxels : default;
        }

        /// <summary>Enregistre le handle du job de meshing en cours pour ce chunk (voir <see cref="ChunkMeshBuilder"/>).</summary>
        public void SetMeshHandle(int3 coord, JobHandle handle)
        {
            meshHandles[coord] = handle;
        }

        /// <summary>À appeler une fois le mesh d'un chunk matérialisé (job terminé et consommé).</summary>
        public void ClearMeshHandle(int3 coord)
        {
            meshHandles.Remove(coord);
        }

        /// <summary>Retire le chunk du monde et libère sa mémoire native (la génération est déterministe :
        /// il sera régénéré à l'identique si on revient).</summary>
        public void RemoveChunk(int3 coord)
        {
            if (!chunks.Remove(coord, out Chunk chunk))
            {
                return;
            }

            // Sécurité : termine tout job encore en train de lire/écrire ce
            // chunk avant de libérer sa mémoire — le sien, mais aussi celui des
            // voisins qui pourraient être en train de le lire comme voisin dans
            // leur propre job de meshing.
            CompleteHandle(generationHandles, coord);
            CompleteHandle(meshHandles, coord);
            foreach (int3 offset in FaceOffsets)
            {
                CompleteHandle(meshHandles, coord + offset);
            }

            chunk.Dispose();
        }

        public bool HasChunk(int3 coord)
        {
            return chunks.ContainsKey(coord);
        }

        public Voxel GetVoxel(int3 worldPosition)
        {
            int size = config.ChunkSize;
            var coord = (int3)math.floor((float3)worldPosition / size);

            if (!chunks.TryGetValue(coord, out Chunk chunk))
            {
                return Voxel.Air;
            }

            // Garantit que la génération (job Burst en tâche de fond) est
            // terminée avant de lire les voxels : nécessaire pour tout appelant
            // hors du pipeline de streaming (futur raycast, édition de blocs...).
            if (generationHandles.TryGetValue(coord, out JobHandle handle))
            {
                handle.Complete();
            }

            int3 local = worldPosition - coord * size;
            return chunk.GetVoxel(local.x, local.y, local.z);
        }

        /// <summary>Hauteur de la surface en (x, z) monde — utile pour placer caméra et joueur.</summary>
        public int GetSurfaceHeight(int worldX, int worldZ)
        {
            return generator.SampleHeight(worldX, worldZ);
        }

        /// <summary>Termine tous les jobs en cours et libère tous les chunks (arrêt du monde).</summary>
        public void Dispose()
        {
            foreach (JobHandle handle in generationHandles.Values)
            {
                handle.Complete();
            }

            generationHandles.Clear();

            foreach (JobHandle handle in meshHandles.Values)
            {
                handle.Complete();
            }

            meshHandles.Clear();

            foreach (Chunk chunk in chunks.Values)
            {
                chunk.Dispose();
            }

            chunks.Clear();
        }

        private static void CompleteHandle(Dictionary<int3, JobHandle> handles, int3 coord)
        {
            if (handles.Remove(coord, out JobHandle handle))
            {
                handle.Complete();
            }
        }
    }
}
