using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Streaming des chunks : maintient chargées les colonnes de chunks dans un
    /// disque autour du joueur, les charge par ordre de proximité avec un budget
    /// par frame (pas de gel), et décharge celles devenues trop lointaines.
    /// Classe métier pure : le rendu est délégué via les callbacks.
    /// </summary>
    public sealed class ChunkStreamer
    {
        // Les 4 colonnes adjacentes horizontalement (pas de diagonale : le
        // face culling ne regarde que les 6 faces axe-aligné d'un voxel).
        private static readonly int2[] NeighborOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        };

        private readonly VoxelWorld world;
        private readonly WorldConfig config;

        // Colonnes (x, z) actuellement chargées, en coordonnées de chunks.
        private readonly HashSet<int2> loadedColumns = new();

        // Colonnes à charger, triées de la plus lointaine à la plus proche :
        // on dépile par la fin, donc la plus proche d'abord.
        private readonly List<int2> pending = new();

        private readonly List<int2> unloadBuffer = new();

        // Colonnes dont au moins un chunk a changé de voisinage ce Process()
        // et dont le mesh doit donc être reconstruit (évite les coutures).
        private readonly HashSet<int2> dirtyColumns = new();

        private int2 center;
        private bool hasCenter;

        public ChunkStreamer(VoxelWorld world, WorldConfig config)
        {
            this.world = world;
            this.config = config;
        }

        /// <summary>
        /// Signale la colonne où se trouve le joueur. Ne recalcule le plan de
        /// chargement que si elle a changé.
        /// </summary>
        public void SetCenter(int2 newCenter)
        {
            if (hasCenter && newCenter.Equals(center))
            {
                return;
            }

            center = newCenter;
            hasCenter = true;
            RebuildPending();
        }

        /// <summary>
        /// Charge jusqu'à <paramref name="maxColumns"/> colonnes (les plus proches
        /// d'abord) et décharge celles hors de portée. À appeler chaque frame.
        /// <paramref name="onChunkMeshDirty"/> est appelé pour chaque chunk dont
        /// le mesh doit être (re)construit : les chunks nouvellement chargés,
        /// mais aussi ceux des colonnes voisines déjà chargées dont une face
        /// vient de gagner ou perdre un voisin solide (évite les coutures entre
        /// chunks générés à des frames différentes).
        /// </summary>
        public void Process(int maxColumns, Action<Chunk> onChunkMeshDirty, Action<int3> onChunkUnloaded)
        {
            if (!hasCenter)
            {
                return;
            }

            dirtyColumns.Clear();

            UnloadDistantColumns(onChunkUnloaded);

            int loaded = 0;
            while (loaded < maxColumns && pending.Count > 0)
            {
                int2 column = pending[^1];
                pending.RemoveAt(pending.Count - 1);

                if (!loadedColumns.Add(column))
                {
                    continue;
                }

                for (int cy = 0; cy < config.VerticalChunkCount; cy++)
                {
                    world.RequestChunk(new int3(column.x, cy, column.y));
                }

                MarkDirtyWithLoadedNeighbors(column);
                loaded++;
            }

            foreach (int2 column in dirtyColumns)
            {
                // Une colonne peut avoir été ajoutée à dirty puis déchargée dans
                // le même Process() (voisin d'une colonne unload). Ne pas la
                // ressusciter via RequestChunk — sinon meshes orphelines en traînée.
                if (!loadedColumns.Contains(column))
                {
                    continue;
                }

                for (int cy = 0; cy < config.VerticalChunkCount; cy++)
                {
                    onChunkMeshDirty(world.RequestChunk(new int3(column.x, cy, column.y)));
                }
            }
        }

        // Marque la colonne elle-même, et toute colonne adjacente déjà chargée,
        // comme ayant besoin d'un remesh (leur voisinage vient de changer).
        private void MarkDirtyWithLoadedNeighbors(int2 column)
        {
            dirtyColumns.Add(column);

            foreach (int2 offset in NeighborOffsets)
            {
                int2 neighbor = column + offset;
                if (loadedColumns.Contains(neighbor))
                {
                    dirtyColumns.Add(neighbor);
                }
            }
        }

        private void RebuildPending()
        {
            pending.Clear();
            int radius = config.ViewDistance;

            // Disque (et non carré) de colonnes autour du centre.
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx * dx + dz * dz > radius * radius)
                    {
                        continue;
                    }

                    int2 column = center + new int2(dx, dz);
                    if (!loadedColumns.Contains(column))
                    {
                        pending.Add(column);
                    }
                }
            }

            pending.Sort((a, b) => DistanceSq(b).CompareTo(DistanceSq(a)));
        }

        // Décharge avec une marge d'hystérésis d'un chunk : évite de charger/
        // décharger en boucle une colonne à la frontière quand on fait des
        // allers-retours.
        private void UnloadDistantColumns(Action<int3> onChunkUnloaded)
        {
            int unloadRadius = config.ViewDistance + 1;
            int unloadRadiusSq = unloadRadius * unloadRadius;

            unloadBuffer.Clear();
            foreach (int2 column in loadedColumns)
            {
                if (DistanceSq(column) > unloadRadiusSq)
                {
                    unloadBuffer.Add(column);
                }
            }

            foreach (int2 column in unloadBuffer)
            {
                loadedColumns.Remove(column);
                dirtyColumns.Remove(column);

                for (int cy = 0; cy < config.VerticalChunkCount; cy++)
                {
                    var coord = new int3(column.x, cy, column.y);
                    world.RemoveChunk(coord);
                    onChunkUnloaded(coord);
                }

                // Les colonnes voisines encore chargées (et non prévues à l'unload)
                // perdent un voisin solide : leurs faces bordant la colonne
                // déchargée doivent réapparaître.
                foreach (int2 offset in NeighborOffsets)
                {
                    int2 neighbor = column + offset;
                    if (loadedColumns.Contains(neighbor))
                    {
                        dirtyColumns.Add(neighbor);
                    }
                }
            }
        }

        private int DistanceSq(int2 column)
        {
            int2 delta = column - center;
            return delta.x * delta.x + delta.y * delta.y;
        }
    }
}
