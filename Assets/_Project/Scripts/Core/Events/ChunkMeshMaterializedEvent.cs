using UnityEngine;

namespace CubeWorld.Core
{
    /// <summary>Publié par World.WorldBootstrap chaque fois qu'un mesh de chunk est matérialisé. Payload primitif : Core ne référence pas World.</summary>
    public readonly struct ChunkMeshMaterializedEvent : IGameEvent
    {
        public readonly Vector3Int ChunkCoord;
        public readonly Vector3 WorldOrigin;

        public ChunkMeshMaterializedEvent(Vector3Int chunkCoord, Vector3 worldOrigin)
        {
            ChunkCoord = chunkCoord;
            WorldOrigin = worldOrigin;
        }
    }
}
