using CubeWorld.CharacterModel;
using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player.CharacterModel;
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
        // Sous cette hauteur monde, le joueur est tombé hors du monde : respawn en surface.
        private const float KillPlaneY = -10f;

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

        [Tooltip("Item (Category = Tool) équipé par défaut au démarrage — sans lui, PlayerMining refuse de miner.")]
        [SerializeField] private ItemDefinition _startingToolItem;

        [Header("Minage")]
        [Tooltip("Item donné en minant une veine de VoxelType.OreCopper (voir CaveShape).")]
        [SerializeField] private ItemDefinition _copperOreItem;

        [Tooltip("Item donné en minant une veine de VoxelType.OreIron (voir CaveShape).")]
        [SerializeField] private ItemDefinition _ironOreItem;

        [Tooltip("Item donné en minant une veine de VoxelType.OreGold (voir CaveShape).")]
        [SerializeField] private ItemDefinition _goldOreItem;

        [Header("Apparence")]
        [Tooltip("Silhouette voxel du joueur (couleurs + accessoires).")]
        [SerializeField]
        private CharacterArchetype _playerArchetype = CharacterArchetype.Swordsman;

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

            if (!loaded && _startingToolItem != null && inventory.TryAdd(_startingToolItem, 1))
            {
                equipment.EquipTool(_startingToolItem);
            }

            if (_itemCatalog == null)
            {
                Debug.LogWarning(
                    "[CubeWorld] PlayerBootstrap : _itemCatalog non assigné — pas de save/load inventaire."
                );
            }

            playerObject.AddComponent<PlayerCombat>().BindInput(_inputActions, equipment, player.CombatEvents);
            playerObject.AddComponent<PlayerGearVisual>().Bind(equipment, player.CharacterModel, player.CombatEvents);
            health.BindEquipment(equipment);
            playerObject.AddComponent<PlayerLoot>().BindInput(_inputActions, inventory);
            playerObject.AddComponent<PlayerMining>().Bind(
                _inputActions,
                _worldBootstrap,
                equipment,
                player.CombatEvents,
                _copperOreItem,
                _ironOreItem,
                _goldOreItem
            );
            playerObject.AddComponent<PlayerCrafting>().Bind(inventory);
            playerObject.AddComponent<PlayerFootstepDust>().Bind(_worldBootstrap);
            PlayerContext.Transform = playerObject.transform;
            PlayerContext.Radius = player.Radius;

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

            // Filet de sécurité : si le joueur est passé sous le monde malgré tout
            // (le terrain commence à y = 0), on le remet sur la surface de sa colonne.
            if (!flyMode && player != null && player.transform.position.y < KillPlaneY)
            {
                Vector3 position = player.transform.position;
                PlacePlayerOnSurface(position.x, position.z);
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
            PlacePlayerOnSurface(camPos.x, camPos.z);
        }

        // Teleport (et non transform.position) : resynchronise le CharacterController
        // et fige le joueur jusqu'à ce que le sol sous lui ait son collider.
        private void PlacePlayerOnSurface(float worldX, float worldZ)
        {
            float groundY = _worldBootstrap.GetSurfaceWorldY(worldX, worldZ);
            player.Teleport(new Vector3(worldX, groundY + 0.1f, worldZ));
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
