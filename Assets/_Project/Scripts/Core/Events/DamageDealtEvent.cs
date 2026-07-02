using UnityEngine;

namespace CubeWorld.Core
{
    /// <summary>Publié par le propriétaire d'un IDamageable (pas par StatBlock) après avoir appliqué des dégâts.</summary>
    public readonly struct DamageDealtEvent : IGameEvent
    {
        public readonly GameObject Target;
        public readonly int Amount;
        public readonly int RemainingHP;

        public DamageDealtEvent(GameObject target, int amount, int remainingHP)
        {
            Target = target;
            Amount = amount;
            RemainingHP = remainingHP;
        }
    }
}
