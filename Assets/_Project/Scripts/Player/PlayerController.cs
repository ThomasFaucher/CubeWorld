using CubeWorld.CharacterModel;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Déplacement du joueur en 3e personne : un <see cref="CharacterController"/>
    /// piloté par <see cref="PlayerMotor"/> (marche, sprint, saut, gravité), avec
    /// l'input lu sur l'action map « Player » de l'InputActionAsset du projet.
    /// Le joueur pivote pour faire face à sa direction de déplacement, calculée
    /// relativement au yaw de la caméra principale (peu importe qui la pilote —
    /// Cinemachine ou la FlyCamera de debug).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        // Portée du rayon qui vérifie qu'il y a du sol solide sous le joueur avant
        // d'activer la gravité (voir IsWaitingForGround).
        private const float GroundProbeDistance = 64f;

        // Au-delà, on relâche quand même le joueur : ne jamais rester bloqué en l'air.
        private const float MaxGroundWait = 10f;

        [Header("Déplacement")]
        [SerializeField]
        private float _walkSpeed = 6f;

        [SerializeField]
        private float _sprintMultiplier = 1.8f;

        [SerializeField]
        private float _jumpHeight = 1.5f;

        [SerializeField]
        private float _gravity = -20f;

        [SerializeField]
        private float _rotationSpeed = 12f;

        [Header("Gabarit")]
        [SerializeField]
        private float _height = 1.8f;

        [SerializeField]
        private float _radius = 0.4f;

        [SerializeField]
        private Vector3 _cameraTargetOffset = new(0f, 1.6f, 0f);

        [Header("Visuel")]
        [Tooltip("Hauteur du mesh voxel (chibi). Plus petit que le collider.")]
        [SerializeField]
        private float _visualHeight = 1.28f;

        [Tooltip(
            "Debug : expression du visage (archétypes procéduraux uniquement, ex. Swordsman)."
        )]
        [SerializeField]
        private CharacterExpression _debugExpression = CharacterExpression.Neutral;

        private CharacterArchetype archetype = CharacterArchetype.Swordsman;
        private int seed;
        private GameObject visualRoot;

        private CharacterController controller;
        private PlayerMotor motor;
        private InputActionMap boundMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private bool waitingForGround;
        private float groundWaitElapsed;

        /// <summary>Rayon de la capsule de collision du joueur (utilisé par les ennemis pour garder leurs distances).</summary>
        public float Radius => _radius;

        /// <summary>Point à hauteur d'épaule suivi/visé par le rig caméra 3e personne.</summary>
        public Transform CameraTarget { get; private set; }

        /// <summary>Rig du personnage voxel généré (os + palette/unité/matériau) — utilisé par PlayerGearVisual pour monter l'arme équipée.</summary>
        public CharacterModelRoot CharacterModel { get; private set; }

        /// <summary>Pont AnimationEvent du layer Combat (voir CharacterCombatAnimationEvents) — bindé par PlayerBootstrap.</summary>
        public CharacterCombatAnimationEvents CombatEvents { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = _height;
            controller.radius = _radius;
            controller.center = new Vector3(0f, _height * 0.5f, 0f);
            controller.stepOffset = 0.6f;
            controller.slopeLimit = 50f;

            motor = new PlayerMotor(_walkSpeed, _sprintMultiplier, _jumpHeight, _gravity);

            var target = new GameObject("CameraTarget");
            target.transform.SetParent(transform, false);
            target.transform.localPosition = _cameraTargetOffset;
            CameraTarget = target.transform;
        }

        /// <summary>Appelé par PlayerBootstrap juste après AddComponent, avant Start.</summary>
        public void Initialize(CharacterArchetype playerArchetype) =>
            Initialize(playerArchetype, seed: 0);

        /// <summary>
        /// Variante avec seed explicite : même seed -> même personnage généré
        /// (couleurs, proportions, cape, pauldron, coiffure pour les archétypes procéduraux).
        /// </summary>
        public void Initialize(CharacterArchetype playerArchetype, int seed)
        {
            archetype = playerArchetype;
            this.seed = seed;
            CreateVoxelVisual();
        }

        /// <summary>Active les actions Move/Jump/Sprint de l'action map « Player » de cet asset.</summary>
        public void BindInput(InputActionAsset inputActions)
        {
            boundMap = inputActions.FindActionMap("Player");
            moveAction = boundMap.FindAction("Move");
            jumpAction = boundMap.FindAction("Jump");
            sprintAction = boundMap.FindAction("Sprint");
            boundMap.Enable();
        }

        private void OnDestroy()
        {
            boundMap?.Disable();
        }

        private void Start()
        {
            // WorldBootstrap.Start pose le joueur sur la surface, mais les colliders de
            // chunk n'arrivent que quelques frames plus tard (meshing + cuisson en fond).
            BeginGroundWait();
        }

        /// <summary>
        /// Déplace le joueur instantanément. Passer par ici plutôt que par
        /// <c>transform.position</c> : avec Auto Sync Transforms désactivé, le
        /// CharacterController garderait sinon son ancienne position interne et le
        /// prochain <c>Move</c> ramènerait le joueur en arrière.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            transform.position = position;
            Physics.SyncTransforms();
            motor.ResetVerticalVelocity();
            BeginGroundWait();
        }

        private void BeginGroundWait()
        {
            waitingForGround = true;
            groundWaitElapsed = 0f;
        }

        // Tant qu'aucun collider n'est détecté sous le joueur (chunk pas encore
        // matérialisé), on le laisse figé : appliquer la gravité le ferait passer
        // à travers le terrain avant que celui-ci ne devienne solide.
        private bool IsWaitingForGround()
        {
            if (!waitingForGround)
            {
                return false;
            }

            // Origine à l'intérieur de la capsule du CharacterController : un raycast
            // ne détecte pas le collider dans lequel il démarre, donc pas le joueur.
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (
                Physics.Raycast(
                    origin,
                    Vector3.down,
                    GroundProbeDistance,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                waitingForGround = false;
                return false;
            }

            groundWaitElapsed += Time.deltaTime;
            if (groundWaitElapsed >= MaxGroundWait)
            {
                Debug.LogWarning(
                    $"[CubeWorld] PlayerController : aucun sol détecté après {MaxGroundWait} s, gravité réactivée."
                );
                waitingForGround = false;
                return false;
            }

            return true;
        }

        private void Update()
        {
            if (moveAction == null || IsWaitingForGround())
            {
                return;
            }

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool sprint = sprintAction.IsPressed();
            bool jumpPressed = jumpAction.WasPressedThisFrame();
            float cameraYaw =
                Camera.main != null ? Camera.main.transform.eulerAngles.y : transform.eulerAngles.y;

            Vector3 move = motor.ComputeMove(
                moveInput,
                sprint,
                jumpPressed,
                controller.isGrounded,
                cameraYaw,
                Time.deltaTime
            );
            CollisionFlags collisions = controller.Move(move);
            motor.NotifyCollisions(collisions);

            if (motor.LastMoveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    motor.LastMoveDirection,
                    Vector3.up
                );
                // Lissage exponentiel : même vitesse de rotation quel que soit le framerate
                // (un simple speed * deltaTime tourne plus vite à bas FPS).
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime)
                );
            }
        }

        // Représentation visuelle : personnage voxel généré en code (style chibi
        // CubeWorld), assemblé en pièces indépendantes (une par partie du corps),
        // animées par un vrai Animator Controller (voir CharacterLocomotionAnimator).
        // Pas de collider dessus : le CharacterController gère la physique du joueur lui-même.
        private void CreateVoxelVisual()
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot);
            }

            visualRoot = CharacterModelBuilder.Build(
                _visualHeight,
                archetype,
                seed,
                _debugExpression
            );
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = Vector3.zero;

            CharacterModel = visualRoot.GetComponent<CharacterModelRoot>();
            var animator = visualRoot.AddComponent<CharacterLocomotionAnimator>();
            animator.Initialize(CharacterModel, controller);

            // Même GameObject que l'Animator (pas celui du joueur) : les AnimationEvent bakés
            // dans les clips de combat/minage ne peuvent appeler que des méthodes portées par
            // le GameObject de l'Animator lui-même — voir CharacterCombatAnimationEvents.
            CombatEvents = visualRoot.AddComponent<CharacterCombatAnimationEvents>();
            CombatEvents.Initialize(animator.Animator);
        }
    }
}
