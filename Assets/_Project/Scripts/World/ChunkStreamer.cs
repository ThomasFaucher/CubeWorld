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
        private readonly VoxelWorld world;
        private readonly WorldConfig config;

        // Colonnes (x, z) actuellement chargées, en coordonnées de chunks.
        private readonly HashSet<int2> loadedColumns = new();

        // Colonnes à charger, triées de la plus lointaine à la plus proche :
        // on dépile par la fin, donc la plus proche d'abord.
        private readonly List<int2> pending = new();

        private readonly List<int2> unloadBuffer = new();

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
        /// </summary>
        public void Process(int maxColumns, Action<Chunk> onChunkLoaded, Action<int3> onChunkUnloaded)
        {
            if (!hasCenter)
            {
                return;
            }

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
                    onChunkLoaded(world.CreateChunk(new int3(column.x, cy, column.y)));
                }

                loaded++;
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
                for (int cy = 0; cy < config.VerticalChunkCount; cy++)
                {
                    var coord = new int3(column.x, cy, column.y);
                    world.RemoveChunk(coord);
                    onChunkUnloaded(coord);
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
