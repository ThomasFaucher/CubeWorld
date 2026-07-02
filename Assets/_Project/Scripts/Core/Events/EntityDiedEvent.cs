using UnityEngine;

namespace CubeWorld.Core
{
    public readonly struct EntityDiedEvent : IGameEvent
    {
        public readonly GameObject Entity;

        public EntityDiedEvent(GameObject entity)
        {
            Entity = entity;
        }
    }
}
