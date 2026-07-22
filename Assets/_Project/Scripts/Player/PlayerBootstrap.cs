using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.World;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Point d'entrée du joueur : instancie en code le joueur (CharacterController
    /// + caméra 3e personne Cinemachine), le branche comme cible de streaming du
    /// monde, et gère un mode debug togglable qui rend la main à la FlyCamera
    /// pour explorer librement (touche F).
    /// </summary>
    public sealed class PlayerBootstrap : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Le monde autour duquel le joueur sera placé et suivi.")]
        [SerializeField] private WorldBootstrap _worldBootstrap;

        [Tooltip("Asset d'actions contenant l'action map « Player » (Move, Look, Jump, Sprint, Attack, Interact).")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Combat")]
        [Tooltip("Item (Category = Weapon) équipé par défaut au démarrage.")]
        [SerializeField] private ItemDefinition _startingWeaponItem;

        [Tooltip("Catalogue Id → ItemDefinition pour le save/load inventaire.")]
        [SerializeField] private ItemCatalog _itemCatalog;

        [Header("Apparence")]
        [Tooltip("Silhouette voxel du joueur (couleurs + accessoires).")]
        [SerializeField]
        private PlayerArchetype _playerArchetype = PlayerArchetype.Swordsman;

        [Tooltip("Seed du personnage généré (archétypes procéduraux, ex. Swordsman). Fixe-le pour itérer sur un visuel précis.")]
        [SerializeField]
        private int _characterSeed;

        [Tooltip("Coché : un personnage différent à chaque lancement. Décoché : toujours _characterSeed.")]
        [SerializeField]
        private bool _randomizeSeedOnSpawn;

        [Header("Caméra")]
        [Tooltip("Distance de la caméra derrière le joueur.")]
        [SerializeField] private float _cameraDistance = 6f;

        [Tooltip("Sensibilité de la souris pour orbiter la caméra. Modifiable en Play mode pour trouver le bon réglage.")]
        [SerializeField] private float _lookSensitivity = 0.05f;

        private PlayerController player;
        private PlayerCameraRig cameraRig;
        private CinemachineBrain brain;
        private FlyCamera flyCamera;
        private bool flyMode;

        private void Awake()
        {
            if (_worldBootstrap == null || _inputActions == null)
            {
                Debug.LogError("[CubeWorld] PlayerBootstrap : _worldBootstrap et _inputActions doivent être assignés dans l'inspecteur.");
                return;
            }

            int effectiveSeed = _randomizeSeedOnSpawn
                ? UnityEngine.Random.Range(int.MinValue, int.MaxValue)
                : _characterSeed;

            var playerObject = new GameObject("Player");
            player = playerObject.AddComponent<PlayerController>();
            player.Initialize(_playerArchetype, effectiveSeed);
            player.BindInput(_inputActions);

            var health = playerObject.AddComponent<PlayerHealth>();
            var inventory = playerObject.AddComponent<PlayerInventory>();
            var hotbar = playerObject.AddComponent<PlayerHotbar>();
            hotbar.Bind(inventory);
            var equipment = playerObject.AddComponent<PlayerEquipment>();
            equipment.Bind(inventory, health);

            var saveService = playerObject.AddComponent<InventorySaveService>();
            saveService.Bind(_itemCatalog, inventory, equipment, hotbar);

            bool loaded = _itemCatalog != null && saveService.TryLoad();
            if (!loaded && _startingWeaponItem != null && inventory.TryAdd(_startingWeaponItem, 1))
            {
                equipment.EquipWeapon(_startingWeaponItem);
            }

            if (_itemCatalog == null)
            {
                Debug.LogWarning(
                    "[CubeWorld] PlayerBootstrap : _itemCatalog non assigné — pas de save/load inventaire."
                );
            }

            playerObject.AddComponent<PlayerCombat>().BindInput(_inputActions, equipment);
            health.BindEquipment(equipment);
            playerObject.AddComponent<PlayerLoot>().BindInput(_inputActions, inventory);
            playerObject.AddComponent<PlayerCrafting>().Bind(inventory);
            playerObject.AddComponent<PlayerFootstepDust>().Bind(_worldBootstrap);
            PlayerContext.Transform = playerObject.transform;

            cameraRig = PlayerCameraRig.Create(player.CameraTarget, _inputActions, _cameraDistance, _lookSensitivity);

            // PlayerCameraRig.Create ajoute le CinemachineBrain sur Camera.main s'il n'existait pas.
            brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            flyCamera = Camera.main != null ? Camera.main.GetComponent<FlyCamera>() : null;
            SetFlyMode(false);

            // Appelé depuis Awake : garanti de s'exécuter avant WorldBootstrap.Start(),
            // qui place la cible sur le sol (voir WorldBootstrap.SetViewTarget).
            _worldBootstrap.SetViewTarget(playerObject.transform);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                SetFlyMode(!flyMode);
            }

            // Permet d'ajuster _lookSensitivity dans l'inspecteur pendant le Play
            // mode et de voir l'effet immédiatement, sans redémarrer.
            if (cameraRig != null)
            {
                cameraRig.LookSensitivity = _lookSensitivity;
            }
        }

        private void SetFlyMode(bool isFlyMode)
        {
            // Quitter le vol : ramener le joueur sous la caméra avant de
            // réactiver le contrôle (sinon on « téléporte » visuellement en arrière).
            if (!isFlyMode && flyMode)
            {
                SnapPlayerToCamera();
            }

            flyMode = isFlyMode;

            if (player != null)
            {
                player.enabled = !isFlyMode;
            }

            if (cameraRig != null)
            {
                cameraRig.gameObject.SetActive(!isFlyMode);
            }

            if (brain != null)
            {
                brain.enabled = !isFlyMode;
            }

            if (flyCamera != null)
            {
                flyCamera.enabled = isFlyMode;
            }

            // Le monde streame autour de ViewTarget. En vol libre la caméra part
            // sans le joueur : il faut suivre la caméra, sinon les chunks restent
            // collés à l'ancienne position du joueur.
            SyncWorldViewTarget();
        }

        private void SnapPlayerToCamera()
        {
            if (player == null || Camera.main == null || _worldBootstrap == null)
            {
                return;
            }

            Vector3 camPos = Camera.main.transform.position;
            int worldX = Mathf.FloorToInt(camPos.x);
            int worldZ = Mathf.FloorToInt(camPos.z);
            int surface = _worldBootstrap.World.GetSurfaceHeight(worldX, worldZ);
            float groundY = (surface + 1) * _worldBootstrap.Config.VoxelSize;
            player.transform.position = new Vector3(camPos.x, groundY + 0.1f, camPos.z);
        }

        private void SyncWorldViewTarget()
        {
            if (_worldBootstrap == null)
            {
                return;
            }

            if (flyMode && Camera.main != null)
            {
                _worldBootstrap.SetViewTarget(Camera.main.transform);
                return;
            }

            if (player != null)
            {
                _worldBootstrap.SetViewTarget(player.transform);
            }
        }
    }
}
