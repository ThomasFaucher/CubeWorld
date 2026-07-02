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
        [Header("Déplacement")]
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _sprintMultiplier = 1.8f;
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _rotationSpeed = 12f;

        [Header("Gabarit")]
        [SerializeField] private float _height = 1.8f;
        [SerializeField] private float _radius = 0.4f;
        [SerializeField] private Vector3 _cameraTargetOffset = new(0f, 1.6f, 0f);

        private CharacterController controller;
        private PlayerMotor motor;
        private InputActionMap boundMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        /// <summary>Point à hauteur d'épaule suivi/visé par le rig caméra 3e personne.</summary>
        public Transform CameraTarget { get; private set; }

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

            CreatePlaceholderVisual();
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

        private void Update()
        {
            if (moveAction == null)
            {
                return;
            }

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool sprint = sprintAction.IsPressed();
            bool jumpPressed = jumpAction.WasPressedThisFrame();
            float cameraYaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : transform.eulerAngles.y;

            Vector3 move = motor.ComputeMove(moveInput, sprint, jumpPressed, controller.isGrounded, cameraYaw, Time.deltaTime);
            controller.Move(move);

            if (motor.LastMoveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(motor.LastMoveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        // Représentation visuelle temporaire (une capsule colorée), en attendant
        // un vrai modèle de personnage. Pas de collider dessus : le
        // CharacterController gère déjà la physique du joueur lui-même.
        private void CreatePlaceholderVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlaceholderVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, _height * 0.5f, 0f);
            visual.transform.localScale = new Vector3(_radius * 2f, _height * 0.5f, _radius * 2f);
            Destroy(visual.GetComponent<Collider>());

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                visual.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = new Color(0.85f, 0.35f, 0.2f) };
            }
        }
    }
}
