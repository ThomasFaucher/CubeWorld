using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Caméra libre provisoire pour explorer le monde avant le vrai joueur.
    /// ZQSD/WASD pour se déplacer, clic droit maintenu + souris pour regarder,
    /// Espace/Ctrl pour monter/descendre, Maj pour accélérer.
    /// </summary>
    public sealed class FlyCamera : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _sprintMultiplier = 4f;
        [SerializeField] private float _lookSensitivity = 0.15f;

        private float yaw;
        private float pitch;

        private void OnEnable()
        {
            // Reprendre l'orientation actuelle (ex. sortie de Cinemachine → vol).
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                return;
            }

            Look(mouse);
            Move(keyboard);
        }

        private void Look(Mouse mouse)
        {
            if (!mouse.rightButton.isPressed)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * _lookSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void Move(Keyboard keyboard)
        {
            // Les touches sont testées par caractère : Z/Q (azerty) et W/A
            // (qwerty) fonctionnent tous les deux.
            var direction = new Vector3(
                Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed || keyboard.qKey.isPressed),
                Axis(keyboard.spaceKey.isPressed, keyboard.leftCtrlKey.isPressed),
                Axis(keyboard.wKey.isPressed || keyboard.zKey.isPressed, keyboard.sKey.isPressed));

            if (direction == Vector3.zero)
            {
                return;
            }

            float speed = _moveSpeed * (keyboard.leftShiftKey.isPressed ? _sprintMultiplier : 1f);

            // Le déplacement suit l'orientation de la caméra (voler vers où on regarde).
            transform.position += transform.TransformDirection(direction.normalized) * (speed * Time.deltaTime);
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
