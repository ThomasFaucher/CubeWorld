using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Logique de déplacement du joueur, indépendante d'Unity/MonoBehaviour :
    /// calcule chaque frame le vecteur à passer à <c>CharacterController.Move</c>,
    /// à partir de l'input brut, du yaw de la caméra et de l'état au sol.
    /// Gère la marche, le sprint, le saut (avec coyote time et mémoire d'appui),
    /// la gravité (plafonnée) et l'arrêt net contre un plafond.
    /// </summary>
    public sealed class PlayerMotor
    {
        // Petite vitesse verticale négative constante au sol : sans elle,
        // CharacterController.isGrounded devient peu fiable (la gravité met
        // plusieurs frames à recoller le contrôleur au sol après un pas).
        private const float GroundedStickVelocity = -2f;

        /// <summary>Vitesse de chute maximale (m/s) : évite une accélération sans fin en cas de longue chute.</summary>
        public const float MaxFallSpeed = 50f;

        /// <summary>Délai (s) pendant lequel un saut reste accepté après avoir quitté le sol (bord de bloc, descente de marche).</summary>
        public const float CoyoteTime = 0.12f;

        /// <summary>Délai (s) pendant lequel un appui sur Saut est mémorisé avant l'atterrissage.</summary>
        public const float JumpBufferTime = 0.12f;

        private readonly float walkSpeed;
        private readonly float sprintMultiplier;
        private readonly float jumpHeight;
        private readonly float gravity;

        private float verticalVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;

        public PlayerMotor(float walkSpeed, float sprintMultiplier, float jumpHeight, float gravity)
        {
            this.walkSpeed = walkSpeed;
            this.sprintMultiplier = sprintMultiplier;
            this.jumpHeight = jumpHeight;
            this.gravity = gravity;
        }

        /// <summary>
        /// Direction horizontale du dernier déplacement demandé (zéro si aucun input).
        /// Norme ≤ 1 : peut être inférieure à 1 avec un stick analogique à mi-course.
        /// </summary>
        public Vector3 LastMoveDirection { get; private set; }

        /// <summary>Vitesse verticale courante (m/s), positive vers le haut.</summary>
        public float VerticalVelocity => verticalVelocity;

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

            coyoteTimer = isGrounded ? CoyoteTime : coyoteTimer - deltaTime;
            jumpBufferTimer = jumpPressed ? JumpBufferTime : jumpBufferTimer - deltaTime;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity = JumpVelocity();
                // Consommés tous les deux : pas de second saut en l'air avec le même
                // appui, ni grâce au coyote time restant.
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
            else if (isGrounded)
            {
                verticalVelocity = GroundedStickVelocity;
            }
            else
            {
                verticalVelocity = Mathf.Max(verticalVelocity + gravity * deltaTime, -MaxFallSpeed);
            }

            float speed = walkSpeed * (sprint ? sprintMultiplier : 1f);
            Vector3 move = horizontalDirection * speed;
            move.y = verticalVelocity;

            return move * deltaTime;
        }

        /// <summary>
        /// À appeler avec le résultat de <c>CharacterController.Move</c> : une tête qui
        /// touche un plafond pendant un saut coupe la montée net, au lieu de « coller »
        /// sous le bloc jusqu'à ce que la gravité annule la vitesse.
        /// </summary>
        public void NotifyCollisions(CollisionFlags flags)
        {
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
        }

        /// <summary>Remet la vitesse verticale à zéro (après une téléportation, par ex.).</summary>
        public void ResetVerticalVelocity()
        {
            verticalVelocity = 0f;
        }

        private float JumpVelocity()
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
