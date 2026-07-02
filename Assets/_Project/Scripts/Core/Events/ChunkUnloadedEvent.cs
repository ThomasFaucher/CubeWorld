using UnityEngine;

namespace CubeWorld.Core
{
    public readonly struct ChunkUnloadedEvent : IGameEvent
    {
        public readonly Vector3Int ChunkCoord;

        public ChunkUnloadedEvent(Vector3Int chunkCoord)
        {
            ChunkCoord = chunkCoord;
        }
    }
}
