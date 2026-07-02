using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Logique de déplacement du joueur, indépendante d'Unity/MonoBehaviour :
    /// calcule chaque frame le vecteur à passer à <c>CharacterController.Move</c>,
    /// à partir de l'input brut, du yaw de la caméra et de l'état au sol.
    /// Gère la marche, le sprint, le saut et la gravité.
    /// </summary>
    public sealed class PlayerMotor
    {
        // Petite vitesse verticale négative constante au sol : sans elle,
        // CharacterController.isGrounded devient peu fiable (la gravité met
        // plusieurs frames à recoller le contrôleur au sol après un pas).
        private const float GroundedStickVelocity = -2f;

        private readonly float walkSpeed;
        private readonly float sprintMultiplier;
        private readonly float jumpHeight;
        private readonly float gravity;

        private float verticalVelocity;

        public PlayerMotor(float walkSpeed, float sprintMultiplier, float jumpHeight, float gravity)
        {
            this.walkSpeed = walkSpeed;
            this.sprintMultiplier = sprintMultiplier;
            this.jumpHeight = jumpHeight;
            this.gravity = gravity;
        }

        /// <summary>Direction horizontale normalisée du dernier déplacement demandé (zéro si aucun input).</summary>
        public Vector3 LastMoveDirection { get; private set; }

        /// <summary>
        /// Calcule le déplacement (en unités monde, déjà multiplié par <paramref name="deltaTime"/>)
        /// à appliquer via <c>CharacterController.Move</c>.
        /// </summary>
        public Vector3 ComputeMove(Vector2 moveInput, bool sprint, bool jumpPressed, bool isGrounded, float cameraYaw, float deltaTime)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);
            Vector3 horizontalDirection = yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
            if (horizontalDirection.sqrMagnitude > 1f)
            {
                horizontalDirection.Normalize();
            }

            LastMoveDirection = horizontalDirection;

            if (isGrounded)
            {
                verticalVelocity = jumpPressed ? JumpVelocity() : GroundedStickVelocity;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            float speed = walkSpeed * (sprint ? sprintMultiplier : 1f);
            Vector3 move = horizontalDirection * speed;
            move.y = verticalVelocity;

            return move * deltaTime;
        }

        private float JumpVelocity()
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
