using System.Collections.Generic;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Le monde voxel : possède les chunks, pilote leur génération et fournit
    /// l'accès global aux voxels par coordonnées monde. Classe métier pure ;
    /// le chargement/déchargement est orchestré par <see cref="ChunkStreamer"/>.
    /// </summary>
    public sealed class VoxelWorld : IVoxelLookup
    {
        private readonly Dictionary<int3, Chunk> chunks = new();
        private readonly WorldConfig config;
        private readonly TerrainGenerator generator;

        public VoxelWorld(WorldConfig config)
        {
            this.config = config;
            generator = new TerrainGenerator(config);
        }

        public IReadOnlyCollection<Chunk> Chunks => chunks.Values;

        /// <summary>Crée et génère le chunk à cette coordonnée (ou renvoie l'existant).</summary>
        public Chunk CreateChunk(int3 coord)
        {
            if (chunks.TryGetValue(coord, out Chunk existing))
            {
                return existing;
            }

            var chunk = new Chunk(coord, config.ChunkSize);
            generator.Generate(chunk);
            chunks[coord] = chunk;
            return chunk;
        }

        /// <summary>Retire le chunk du monde (sa mémoire est libérée ; il sera
        /// régénéré à l'identique si on revient, la génération étant déterministe).</summary>
        public void RemoveChunk(int3 coord)
        {
            chunks.Remove(coord);
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

            int3 local = worldPosition - coord * size;
            return chunk.GetVoxel(local.x, local.y, local.z);
        }

        /// <summary>Hauteur de la surface en (x, z) monde — utile pour placer caméra et joueur.</summary>
        public int GetSurfaceHeight(int worldX, int worldZ)
        {
            return generator.SampleHeight(worldX, worldZ);
        }
    }
}
