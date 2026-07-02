using UnityEngine;

namespace CubeWorld.Core
{
    /// <summary>Publié par World.NavMeshRegionBaker après chaque (re)bake réussi. Combat s'y abonne pour spawner/nettoyer dans la zone bakée sans référencer World.</summary>
    public readonly struct NavMeshBakedEvent : IGameEvent
    {
        public readonly Vector3 Center;
        public readonly Vector3 Size;
        public readonly int Version;

        public NavMeshBakedEvent(Vector3 center, Vector3 size, int version)
        {
            Center = center;
            Size = size;
            Version = version;
        }
    }
}
