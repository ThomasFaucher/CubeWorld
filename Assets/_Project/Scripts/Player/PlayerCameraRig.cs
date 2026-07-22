using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Caméra 3e personne orbitale (Cinemachine 3.1) : orbite autour d'une
    /// cible selon l'action « Look » (souris/stick droit), toujours centrée
    /// dessus. Construite entièrement en code au runtime — comme
    /// WorldBootstrap le fait pour les chunks — pour éviter d'avoir à câbler
    /// des composants Cinemachine à la main dans la scène.
    /// </summary>
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        // Pitch négatif = caméra sous la cible → plus de ciel dans le cadre.
        // Le Decollider (TerrainResolution) empêche de passer sous le sol.
        private const float MinPitch = -30f;
        private const float MaxPitch = 60f;
        private const float DefaultPitch = 15f;

        // Valeurs par défaut si Create() est appelé sans les préciser — en
        // pratique toujours surchargées par PlayerBootstrap, qui les expose
        // dans l'inspecteur (le rig, lui, n'existe qu'à l'exécution : ses
        // propres champs sérialisés ne sont donc pas éditables avant le Play).
        private const float DefaultDistance = 6f;
        private const float DefaultLookSensitivity = 0.05f;

        private float lookSensitivity;

        private CinemachineOrbitalFollow orbitalFollow;
        private InputAction lookAction;

        /// <summary>Sensibilité de la souris, modifiable à la volée (ex. depuis l'inspecteur en Play mode).</summary>
        public float LookSensitivity
        {
            get => lookSensitivity;
            set => lookSensitivity = value;
        }

        /// <summary>Crée le rig caméra (Cinemachine + CinemachineBrain sur la caméra principale) suivant la cible donnée.</summary>
        public static PlayerCameraRig Create(Transform followTarget, InputActionAsset inputActions, float distance = DefaultDistance, float lookSensitivity = DefaultLookSensitivity)
        {
            var rigObject = new GameObject("PlayerCameraRig");
            var rig = rigObject.AddComponent<PlayerCameraRig>();
            rig.lookSensitivity = lookSensitivity;
            rig.Initialize(followTarget, inputActions, distance);
            return rig;
        }

        private void Initialize(Transform followTarget, InputActionAsset inputActions, float distance)
        {
            EnsureBrainOnMainCamera();

            var cmCamera = gameObject.AddComponent<CinemachineCamera>();
            cmCamera.Follow = followTarget;
            cmCamera.LookAt = followTarget;

            orbitalFollow = gameObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.Radius = distance;

            orbitalFollow.HorizontalAxis.Value = 0f;
            orbitalFollow.HorizontalAxis.Center = 0f;
            orbitalFollow.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbitalFollow.HorizontalAxis.Wrap = true;

            orbitalFollow.VerticalAxis.Value = DefaultPitch;
            orbitalFollow.VerticalAxis.Center = DefaultPitch;
            orbitalFollow.VerticalAxis.Range = new Vector2(MinPitch, MaxPitch);
            orbitalFollow.VerticalAxis.Wrap = false;

            var rotationComposer = gameObject.AddComponent<CinemachineRotationComposer>();

            // AddComponent() n'appelle pas Reset() (ça, c'est un helper Editor-only
            // déclenché par l'ajout depuis l'Inspector) : Damping n'a pas d'initialiseur
            // de champ et reste donc à (0,0) si on ne le fixe pas nous-mêmes, ce qui
            // vise instantanément la cible chaque frame pendant que la position de la
            // caméra (OrbitalFollow, elle bien amortie) traîne derrière — d'où le
            // mouvement de caméra bizarre uniquement quand le joueur se déplace.
            rotationComposer.Damping = new Vector2(0.5f, 0.5f);

            // Remonte la caméra au-dessus des MeshCollider des chunks (layer Default)
            // quand l'orbite voudrait la placer sous le sol — sans bloquer le pitch
            // négatif qui sert à cadrer le ciel.
            var decollider = gameObject.AddComponent<CinemachineDecollider>();
            decollider.CameraRadius = 0.35f;
            decollider.TerrainResolution = new CinemachineDecollider.TerrainSettings
            {
                Enabled = true,
                TerrainLayers = 1, // Default — colliders des chunks (WorldBootstrap)
                MaximumRaycast = Mathf.Max(10f, distance + 4f),
                Damping = 0.15f,
            };
            decollider.Decollision = new CinemachineDecollider.DecollisionSettings
            {
                Enabled = false,
            };

            InputActionMap map = inputActions.FindActionMap("Player");
            lookAction = map.FindAction("Look");
            map.Enable();
        }

        private static void EnsureBrainOnMainCamera()
        {
            Camera camera = Camera.main;
            if (camera != null && camera.GetComponent<CinemachineBrain>() == null)
            {
                camera.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        private void Update()
        {
            if (lookAction == null)
            {
                return;
            }

            // <Pointer>/delta est déjà un delta par frame (comme la souris de
            // FlyCamera), pas un taux à intégrer : pas de Time.deltaTime ici,
            // sinon la sensibilité explose (dépend en plus du framerate).
            Vector2 look = lookAction.ReadValue<Vector2>();
            float horizontal = orbitalFollow.HorizontalAxis.Value + look.x * lookSensitivity;
            float vertical = orbitalFollow.VerticalAxis.Value - look.y * lookSensitivity;

            orbitalFollow.HorizontalAxis.Value = orbitalFollow.HorizontalAxis.ClampValue(horizontal);
            orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(vertical);
        }
    }
}
