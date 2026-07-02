using CubeWorld.Combat;
using CubeWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Attaque de mêlée du joueur : lit l'action « Attack » (déjà activée par
    /// PlayerController.BindInput), résout les coups via MeleeAttackResolver
    /// (partagée avec Combat.EnemyAI).
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        private PlayerEquipment equipment;
        private InputAction attackAction;
        private readonly MeleeAttackResolver attackResolver = new();
        private float attackTimer;

        /// <summary>Branche l'action Attack et l'équipement (l'arme active est lue à chaque coup, pas figée). Appelé par PlayerBootstrap.</summary>
        public void BindInput(InputActionAsset inputActions, PlayerEquipment playerEquipment)
        {
            attackAction = inputActions.FindActionMap("Player").FindAction("Attack");
            equipment = playerEquipment;
        }

        private void Update()
        {
            attackTimer -= Time.deltaTime;

            if (attackAction == null || equipment == null || equipment.CurrentWeapon == null)
            {
                return;
            }

            if (attackAction.WasPressedThisFrame() && attackTimer <= 0f)
            {
                Attack();
            }
        }

        private void Attack()
        {
            WeaponDefinition weapon = equipment.CurrentWeapon;
            attackTimer = weapon.AttackCooldown;

            var hits = attackResolver.ResolveHits(
                transform.position,
                GetAttackForward(),
                weapon.Range,
                weapon.Radius,
                gameObject
            );
            foreach (IDamageable hit in hits)
            {
                hit.ApplyDamage(new DamageInfo(weapon.Damage));
            }
        }

        private Vector3 GetAttackForward()
        {
            if (Camera.main != null)
            {
                Vector3 cameraForward = Vector3.ProjectOnPlane(
                    Camera.main.transform.forward,
                    Vector3.up
                );
                if (cameraForward.sqrMagnitude > 0.0001f)
                {
                    return cameraForward.normalized;
                }
            }

            return transform.forward;
        }
    }
}
