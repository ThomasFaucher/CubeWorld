using CubeWorld.CharacterModel;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Pilote un vrai <see cref="Animator"/> (Blend Tree Idle/Walk/Run mélangé en continu par
    /// le paramètre float "Speed") posé sur la racine du personnage voxel du joueur. Remplace
    /// l'ancien ProceduralCharacterAnimator (rotation des os recalculée à la main chaque
    /// frame) : les trois clips + l'AnimatorController sont des assets générés une fois pour
    /// toutes par l'outil éditeur <c>CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator</c>
    /// (menu CubeWorld/Character/Generate Locomotion Animator) et chargés ici via
    /// <see cref="Resources.Load{T}"/> — le personnage étant assemblé en code (pas de prefab),
    /// il n'y a pas d'autre endroit où assigner ce RuntimeAnimatorController dans l'inspecteur.
    ///
    /// Nourrit deux paramètres float à partir de la vitesse horizontale du
    /// CharacterController (avec un léger damping, voir <see cref="SpeedDampTime"/>) :
    /// "Speed" (poids du Blend Tree Idle/Walk/Run) et "PlaybackSpeed" (cadence de lecture, voir
    /// <see cref="CadenceReferenceSpeed"/>) — tout le reste (courbes de foulée, inclinaison
    /// avant, seuils du Blend Tree) vit dans les assets générés, pas dans du code.
    /// </summary>
    public sealed class CharacterLocomotionAnimator : MonoBehaviour
    {
        private const string ControllerResourcePath = "Animations/CharacterModel/CharacterLocomotion";
        private const string SpeedParameter = "Speed";
        private const string PlaybackSpeedParameter = "PlaybackSpeed";

        // PlayerMotor.ComputeMove n'accélère pas progressivement : la vitesse horizontale
        // saute instantanément entre 0/marche/sprint selon l'input. Ce damping lisse "Speed"
        // côté Animator pour que le Blend Tree Idle/Walk/Run (voir
        // CharacterLocomotionAnimatorGenerator) transitionne en douceur au lieu de sauter d'une
        // pose à l'autre d'une frame à l'autre.
        private const float SpeedDampTime = 0.12f;

        // Vitesse (m/s) à laquelle le Blend Tree atteint le poids plein de Walk (doit rester
        // cohérente avec WalkBlendSpeed dans CharacterLocomotionAnimatorGenerator — assemblées
        // séparées, pas de constante partagée possible). Sert de référence pour la cadence :
        // au-delà, la lecture accélère proportionnellement à la vitesse réelle, pour que la
        // foulée ne semble jamais "glisser" derrière un déplacement plus rapide que l'animation
        // baked (sprint par défaut ~10.8 m/s, largement au-dessus de cette référence).
        private const float CadenceReferenceSpeed = 5f;

        // Jamais < 1x : sinon, à l'arrêt (vitesse 0), la cadence tomberait à 0 et figerait même
        // la respiration d'Idle (poids plein à vitesse nulle dans le Blend Tree).
        private const float MinPlaybackSpeed = 1f;

        private CharacterController controller;
        private Animator animator;

        /// <summary>Exposé pour CharacterCombatAnimationEvents (posé sur le même GameObject, voir PlayerController.CreateVoxelVisual) et son pilotage du layer "Combat".</summary>
        public Animator Animator => animator;

        /// <param name="characterModel">
        /// Non utilisé directement (les chemins de courbes des clips sont relatifs à cette
        /// racine par construction) — gardé en paramètre pour rester symétrique de l'ancien
        /// ProceduralCharacterAnimator.Initialize et documenter le couplage au rig.
        /// </param>
        public void Initialize(CharacterModelRoot characterModel, CharacterController characterController)
        {
            controller = characterController;

            var runtimeController = Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);
            if (runtimeController == null)
            {
                Debug.LogWarning(
                    "[CubeWorld] CharacterLocomotionAnimator : AnimatorController introuvable à "
                        + $"Resources/{ControllerResourcePath}. Lancer le menu éditeur "
                        + "CubeWorld/Character/Generate Locomotion Animator pour le générer."
                );
                return;
            }

            animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = runtimeController;
        }

        private void LateUpdate()
        {
            if (animator == null || controller == null)
            {
                return;
            }

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            animator.SetFloat(SpeedParameter, speed, SpeedDampTime, Time.deltaTime);

            float playbackSpeed = Mathf.Max(MinPlaybackSpeed, speed / CadenceReferenceSpeed);
            animator.SetFloat(PlaybackSpeedParameter, playbackSpeed, SpeedDampTime, Time.deltaTime);
        }
    }
}
