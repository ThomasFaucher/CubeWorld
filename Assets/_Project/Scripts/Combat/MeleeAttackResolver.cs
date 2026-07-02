using System.Collections.Generic;
using CubeWorld.Core;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Résolution de coups de mêlée, réutilisée par PlayerCombat et EnemyAI :
    /// une sphère projetée devant l'attaquant, dédupliquée, l'attaquant exclu.
    /// </summary>
    public sealed class MeleeAttackResolver
    {
        private readonly List<IDamageable> hitBuffer = new();
        private readonly Collider[] overlapBuffer = new Collider[16];

        public IReadOnlyList<IDamageable> ResolveHits(
            Vector3 origin,
            Vector3 forward,
            float range,
            float radius,
            GameObject attacker,
            float originHeight = 1f
        )
        {
            hitBuffer.Clear();

            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                return hitBuffer;
            }

            flatForward.Normalize();

            // Capsule du torse jusqu'à la portée max : couvre les cibles proches,
            // contrairement à une sphère posée uniquement au bout de la portée.
            Vector3 start = origin + Vector3.up * originHeight;
            Vector3 end = start + flatForward * range;
            int count = Physics.OverlapCapsuleNonAlloc(
                start,
                end,
                radius,
                overlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = overlapBuffer[i];
                if (attacker != null && hitCollider.transform.IsChildOf(attacker.transform))
                {
                    continue;
                }

                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
                if (damageable == null || hitBuffer.Contains(damageable))
                {
                    continue;
                }

                hitBuffer.Add(damageable);
            }

            return hitBuffer;
        }
    }
}
