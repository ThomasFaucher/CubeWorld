using System;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Pont entre le layer Animator "Combat" (voir
    /// CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator) et le gameplay : posé sur le
    /// même GameObject que l'<see cref="Animator"/> du personnage (racine du modèle voxel, PAS
    /// le joueur — les AnimationEvent ne peuvent appeler que des méthodes sur des composants du
    /// GameObject qui porte l'Animator), reçoit les événements bakés dans les clips de combat/
    /// minage et les republie en C# (événements .NET) pour que PlayerCombat/PlayerMining/
    /// PlayerGearVisual (posés sur le GameObject du joueur, un parent différent) puissent s'y
    /// abonner sans que ce script ait besoin de les connaître.
    ///
    /// Pilote aussi le poids du layer "Combat" : <see cref="TriggerAction"/> le monte à 1 au
    /// moment de déclencher une action, et chaque clip de combat/minage baque un événement
    /// générique "OnActionEnd" tout près de sa dernière frame (voir le générateur) qui le
    /// redescend à 0 — si le combo enchaîne (nouveau TriggerAction avant la fin), l'ancien clip
    /// est interrompu par la transition Any State et son OnActionEnd ne se déclenche jamais :
    /// le poids reste à 1 en continu tout le temps du combo, sans à-coup.
    /// </summary>
    public sealed class CharacterCombatAnimationEvents : MonoBehaviour
    {
        private const int CombatLayerIndex = 1;
        private const string ActionIdParameter = "ActionId";
        private const string ActionTriggerParameter = "ActionTrigger";

        private Animator animator;

        public event Action WeaponGrabbed;
        public event Action AttackHit;
        public event Action WeaponSheathed;
        public event Action ToolGrabbed;
        public event Action MineHit;
        public event Action ToolSheathed;

        /// <summary>Appelé par PlayerController juste après avoir créé l'Animator (CharacterLocomotionAnimator.Initialize).</summary>
        public void Initialize(Animator characterAnimator)
        {
            animator = characterAnimator;
        }

        /// <summary>Choisit le clip (voir CombatActionId) et déclenche la transition Any State correspondante.</summary>
        public void TriggerAction(int actionId)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetLayerWeight(CombatLayerIndex, 1f);
            animator.SetInteger(ActionIdParameter, actionId);
            animator.SetTrigger(ActionTriggerParameter);
        }

        // --- Récepteurs d'AnimationEvent (noms exacts référencés par le générateur) ---------

        public void OnWeaponGrabbed() => WeaponGrabbed?.Invoke();

        public void OnAttackHit() => AttackHit?.Invoke();

        public void OnWeaponSheathed() => WeaponSheathed?.Invoke();

        public void OnToolGrabbed() => ToolGrabbed?.Invoke();

        public void OnMineHit() => MineHit?.Invoke();

        public void OnToolSheathed() => ToolSheathed?.Invoke();

        public void OnActionEnd()
        {
            if (animator != null)
            {
                animator.SetLayerWeight(CombatLayerIndex, 0f);
            }
        }
    }
}
