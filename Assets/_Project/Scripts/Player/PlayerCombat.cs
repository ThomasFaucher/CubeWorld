using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player.CharacterModel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Attaque de mêlée du joueur : épée en combo séquentiel (dégainer + jusqu'à 3 coups qui
    /// s'enchaînent, rengainage automatique après un délai d'inactivité) si une arme est
    /// équipée, sinon un combo de coups de poing à mains nues (toujours "prêts", pas de
    /// dégainer/rengainer). Les dégâts ne sont plus résolus à l'instant de la pression du
    /// bouton : chaque clip déclenché (voir <see cref="CharacterCombatAnimationEvents"/>) baque
    /// un AnimationEvent "OnAttackHit" au moment du contact réel, qui appelle
    /// <see cref="ResolveHit"/> — la portée/dégâts touchent au bon moment de l'animation.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        // Délai d'inactivité (pas de nouvelle pression) après lequel le combo épée range
        // automatiquement l'arme (retour à SwordSheath) — voir Update.
        private const float SwordIdleSheathDelay = 1.5f;

        // Combat à mains nues : pas de WeaponDefinition (aucun item associé), valeurs de
        // référence en dur.
        private const int UnarmedDamage = 6;
        private const float UnarmedRange = 1.6f;
        private const float UnarmedRadius = 0.7f;
        private const float UnarmedCooldown = 0.32f;

        private PlayerEquipment equipment;
        private InputAction attackAction;
        private CharacterCombatAnimationEvents combatEvents;
        private readonly MeleeAttackResolver attackResolver = new();

        private float attackTimer;
        private float sheathTimer;
        private bool weaponDrawn;
        private int swordComboStep;
        private int punchComboStep;
        private ItemDefinition trackedWeaponItem;

        private bool pendingHitIsSword;
        private WeaponDefinition pendingWeapon;

        /// <summary>Branche l'action Attack, l'équipement et le pont d'AnimationEvent. Appelé par PlayerBootstrap.</summary>
        public void BindInput(
            InputActionAsset inputActions,
            PlayerEquipment playerEquipment,
            CharacterCombatAnimationEvents animationEvents
        )
        {
            attackAction = inputActions.FindActionMap("Player").FindAction("Attack");
            equipment = playerEquipment;
            combatEvents = animationEvents;

            if (combatEvents != null)
            {
                combatEvents.AttackHit += ResolveHit;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged += OnEquipmentChanged;
                trackedWeaponItem = equipment.WeaponItem;
            }
        }

        private void OnDestroy()
        {
            if (combatEvents != null)
            {
                combatEvents.AttackHit -= ResolveHit;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged -= OnEquipmentChanged;
            }
        }

        // Un changement d'arme (pas n'importe quel changement d'équipement — l'armure ne doit
        // pas interrompre un combo en cours) invalide le combo/l'état "dégainé" en cours :
        // PlayerGearVisual a déjà démonté l'ancienne arme du dos, replaying son combo n'aurait
        // plus de sens.
        private void OnEquipmentChanged()
        {
            if (equipment.WeaponItem == trackedWeaponItem)
            {
                return;
            }

            trackedWeaponItem = equipment.WeaponItem;
            weaponDrawn = false;
            swordComboStep = 0;
        }

        private void Update()
        {
            attackTimer -= Time.deltaTime;

            if (attackAction == null || equipment == null)
            {
                return;
            }

            bool hasSword = equipment.CurrentWeapon != null;

            if (weaponDrawn && hasSword)
            {
                sheathTimer -= Time.deltaTime;
                if (sheathTimer <= 0f)
                {
                    Sheath();
                }
            }

            if (!attackAction.WasPressedThisFrame() || attackTimer > 0f)
            {
                return;
            }

            if (hasSword)
            {
                AttackWithSword(equipment.CurrentWeapon);
            }
            else
            {
                AttackUnarmed();
            }
        }

        private void AttackWithSword(WeaponDefinition weapon)
        {
            attackTimer = weapon.AttackCooldown;
            sheathTimer = SwordIdleSheathDelay;
            pendingHitIsSword = true;
            pendingWeapon = weapon;

            int actionId;
            if (!weaponDrawn)
            {
                // Premier coup depuis le dos : la main va chercher l'épée (voir
                // CharacterCombatAnimationEvents.OnWeaponGrabbed -> PlayerGearVisual).
                weaponDrawn = true;
                swordComboStep = 1;
                actionId = CombatActionId.SwordDrawSlash1;
            }
            else
            {
                // Épée déjà en main : combo séquentiel 1 -> 2 -> 3 -> 1 (voir le plan).
                swordComboStep = swordComboStep >= 3 ? 1 : swordComboStep + 1;
                actionId = swordComboStep switch
                {
                    1 => CombatActionId.SwordSlash1,
                    2 => CombatActionId.SwordSlash2,
                    _ => CombatActionId.SwordSlash3,
                };
            }

            combatEvents?.TriggerAction(actionId);
        }

        private void Sheath()
        {
            weaponDrawn = false;
            swordComboStep = 0;
            combatEvents?.TriggerAction(CombatActionId.SwordSheath);
        }

        private void AttackUnarmed()
        {
            attackTimer = UnarmedCooldown;
            pendingHitIsSword = false;

            punchComboStep = punchComboStep >= 3 ? 1 : punchComboStep + 1;
            int actionId = punchComboStep switch
            {
                1 => CombatActionId.PunchJabL,
                2 => CombatActionId.PunchJabR,
                _ => CombatActionId.PunchCross,
            };

            combatEvents?.TriggerAction(actionId);
        }

        // Appelé par CharacterCombatAnimationEvents.AttackHit (AnimationEvent "OnAttackHit"
        // baké dans le clip en cours, épée ou poing) : c'est ICI, au moment du contact réel de
        // l'animation, que les dégâts sont résolus — plus à l'instant de la pression du bouton.
        private void ResolveHit()
        {
            if (pendingHitIsSword && pendingWeapon != null)
            {
                ApplyHits(pendingWeapon.Range, pendingWeapon.Radius, pendingWeapon.Damage);
            }
            else
            {
                ApplyHits(UnarmedRange, UnarmedRadius, UnarmedDamage);
            }
        }

        private void ApplyHits(float range, float radius, int damage)
        {
            var hits = attackResolver.ResolveHits(
                transform.position,
                GetAttackForward(),
                range,
                radius,
                gameObject
            );
            foreach (IDamageable hit in hits)
            {
                hit.ApplyDamage(new DamageInfo(damage));
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
