using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Animation procédurale de validation du rig : idle (respiration du torse, léger
    /// balancement de tête) et cycle de marche (bras/jambes en opposition, flexion des
    /// avant-bras et des pieds), purement par ROTATION des Transforms des pièces — la preuve
    /// que les pivots/sockets sont bons. Piloté par la vitesse horizontale du
    /// CharacterController du joueur.
    ///
    /// Conçu pour être remplacé plus tard par un Animator Controller : désactiver ce
    /// composant suffit (les noms d'os stables de <see cref="CharacterRigDefinition"/>
    /// permettent de créer les clips dans l'éditeur).
    /// </summary>
    public sealed class ProceduralCharacterAnimator : MonoBehaviour
    {
        [Header("Marche")]
        [Tooltip("Distance parcourue (m) pour un cycle complet de marche.")]
        [SerializeField]
        private float _strideLength = 1.5f;

        [Tooltip("Vitesse (m/s) à laquelle le cycle de marche est à pleine amplitude.")]
        [SerializeField]
        private float _fullSwingSpeed = 4f;

        [SerializeField]
        private float _legSwingDegrees = 32f;

        [SerializeField]
        private float _armSwingDegrees = 38f;

        [Header("Idle")]
        [Tooltip("Amplitude (°) de la respiration du torse à l'arrêt.")]
        [SerializeField]
        private float _breathDegrees = 2f;

        private CharacterModelRoot model;
        private CharacterController controller;

        private Quaternion bindTorso;
        private Quaternion bindHead;
        private Quaternion bindArmL;
        private Quaternion bindArmR;
        private Quaternion bindForearmL;
        private Quaternion bindForearmR;
        private Quaternion bindLegL;
        private Quaternion bindLegR;
        private Quaternion bindFootL;
        private Quaternion bindFootR;

        private float phase;
        private float walkWeight;

        public void Initialize(
            CharacterModelRoot characterModel,
            CharacterController characterController
        )
        {
            model = characterModel;
            controller = characterController;

            // Pose de repos capturée à l'assemblage : toutes les rotations d'animation sont
            // des deltas par rapport à elle.
            bindTorso = model.Torso.localRotation;
            bindHead = model.Head.localRotation;
            bindArmL = model.ArmL.localRotation;
            bindArmR = model.ArmR.localRotation;
            bindForearmL = model.ForearmL.localRotation;
            bindForearmR = model.ForearmR.localRotation;
            bindLegL = model.LegL.localRotation;
            bindLegR = model.LegR.localRotation;
            bindFootL = model.FootL.localRotation;
            bindFootR = model.FootR.localRotation;
        }

        private void LateUpdate()
        {
            if (model == null || controller == null)
            {
                return;
            }

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            // Poids de marche lissé : évite que la pose "claque" au démarrage/à l'arrêt.
            float targetWeight = Mathf.Clamp01(speed / _fullSwingSpeed);
            walkWeight = Mathf.MoveTowards(walkWeight, targetWeight, Time.deltaTime * 6f);

            // La phase avance avec la distance parcourue : la cadence des pas suit
            // naturellement la vitesse (marche lente = pas lents, sprint = pas rapides).
            phase += (speed / Mathf.Max(0.1f, _strideLength)) * Mathf.PI * 2f * Time.deltaTime;

            float swing = Mathf.Sin(phase) * walkWeight;
            float time = Time.time;

            // Jambes en opposition ; les pieds contre-fléchissent pour rester ~parallèles au
            // sol pendant le balancement.
            model.LegL.localRotation =
                bindLegL * Quaternion.Euler(swing * _legSwingDegrees, 0f, 0f);
            model.LegR.localRotation =
                bindLegR * Quaternion.Euler(-swing * _legSwingDegrees, 0f, 0f);
            model.FootL.localRotation =
                bindFootL * Quaternion.Euler(-swing * _legSwingDegrees * 0.5f, 0f, 0f);
            model.FootR.localRotation =
                bindFootR * Quaternion.Euler(swing * _legSwingDegrees * 0.5f, 0f, 0f);

            // Bras opposés à la jambe du même côté, légèrement écartés du corps (pose chibi),
            // avec un micro-balancement résiduel à l'arrêt.
            float idleArmSway = (1f - walkWeight) * Mathf.Sin(time * 1.3f) * 2f;
            model.ArmL.localRotation =
                bindArmL * Quaternion.Euler((-swing * _armSwingDegrees) + idleArmSway, 0f, 5f);
            model.ArmR.localRotation =
                bindArmR * Quaternion.Euler((swing * _armSwingDegrees) + idleArmSway, 0f, -5f);

            // Avant-bras : flexion de base + surcroît quand le bras part vers l'avant
            // (le coude ne s'étend jamais en arrière, comme une vraie marche).
            float bendL = 10f + (Mathf.Max(0f, -swing) * 25f);
            float bendR = 10f + (Mathf.Max(0f, swing) * 25f);
            model.ForearmL.localRotation = bindForearmL * Quaternion.Euler(-bendL, 0f, 0f);
            model.ForearmR.localRotation = bindForearmR * Quaternion.Euler(-bendR, 0f, 0f);

            // Torse : double fréquence en marche (un rebond par pas), respiration à l'arrêt.
            float torsoPitch =
                (walkWeight * Mathf.Sin(phase * 2f) * 2f)
                + ((1f - walkWeight) * Mathf.Sin(time * 1.6f) * _breathDegrees);
            model.Torso.localRotation = bindTorso * Quaternion.Euler(torsoPitch, 0f, 0f);

            // Tête : contre-balancement discret en marche, lent dodelinement à l'arrêt.
            float headYaw = walkWeight * Mathf.Sin(phase) * 2f;
            float headRoll = (1f - walkWeight) * Mathf.Sin(time * 0.9f) * 1.5f;
            model.Head.localRotation = bindHead * Quaternion.Euler(0f, headYaw, headRoll);
        }
    }
}
