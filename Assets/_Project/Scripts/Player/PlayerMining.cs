using System.Collections.Generic;
using CubeWorld.Combat;
using CubeWorld.Player.CharacterModel;
using CubeWorld.World;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Récolte les veines de minerai visées (voir CaveShape, VoxelType.OreCopper/Iron/Gold),
    /// à condition qu'une pioche soit équipée (voir PlayerEquipment.CurrentTool — mains nues,
    /// pas de minage possible). Sur l'action « Interact » (même action que PlayerLoot — les
    /// deux peuvent se déclencher sur la même pression, l'un ramasse ce qui est au sol, l'autre
    /// mine ce qui est visé, sans jamais se gêner), un rayon physique tiré depuis la caméra
    /// (portée comptée depuis le joueur, voir TryRaycastTerrain) cherche le voxel visé ;
    /// s'il s'agit d'un minerai, la cible est mise en cache et l'animation (dégainer + swing, ou juste swing si déjà en main) est déclenchée — le
    /// voxel n'est retiré du monde (VoxelWorld.TrySetVoxel + WorldBootstrap.RequestRemesh) et
    /// l'ItemPickup n'apparaît qu'au contact réel de l'animation (AnimationEvent "OnMineHit",
    /// voir ApplyMine). Pas de bris de bloc générique : seul le minerai est minable, la
    /// pierre/terre reste indestructible (le monde reste stable, aucun système de
    /// construction à gérer côté joueur).
    /// </summary>
    public sealed class PlayerMining : MonoBehaviour
    {
        // Portée de minage, mesurée depuis le centre du joueur (pas depuis la caméra).
        private const float Reach = 4.5f;
        private const float PlayerCenterHeight = 1f;
        private const float SurfaceEpsilon = 0.01f;

        // Délai d'inactivité (pas de nouveau coup) après lequel la pioche est rengainée
        // automatiquement — même logique que PlayerCombat pour l'épée.
        private const float ToolIdleSheathDelay = 1.5f;

        private WorldBootstrap worldBootstrap;
        private PlayerEquipment equipment;
        private CharacterCombatAnimationEvents combatEvents;
        private ItemDefinition copperOreItem;
        private ItemDefinition ironOreItem;
        private ItemDefinition goldOreItem;
        private InputAction interactAction;

        private readonly List<int3> dirtyChunksBuffer = new();
        private readonly RaycastHit[] raycastBuffer = new RaycastHit[16];

        private float mineTimer;
        private float sheathTimer;
        private bool toolDrawn;

        private bool hasPendingTarget;
        private int3 pendingVoxel;
        private VoxelType pendingVoxelType;
        private Vector3 pendingHitPoint;
        private ItemDefinition pendingOreItem;

        /// <summary>Appelé par PlayerBootstrap juste après la création du joueur.</summary>
        public void Bind(
            InputActionAsset inputActions,
            WorldBootstrap worldBootstrap,
            PlayerEquipment equipment,
            CharacterCombatAnimationEvents animationEvents,
            ItemDefinition copperOreItem,
            ItemDefinition ironOreItem,
            ItemDefinition goldOreItem
        )
        {
            interactAction = inputActions.FindActionMap("Player").FindAction("Interact");
            this.worldBootstrap = worldBootstrap;
            this.equipment = equipment;
            combatEvents = animationEvents;
            this.copperOreItem = copperOreItem;
            this.ironOreItem = ironOreItem;
            this.goldOreItem = goldOreItem;

            if (combatEvents != null)
            {
                combatEvents.MineHit += ApplyMine;
            }
        }

        private void OnDestroy()
        {
            if (combatEvents != null)
            {
                combatEvents.MineHit -= ApplyMine;
            }
        }

        private void Update()
        {
            mineTimer -= Time.deltaTime;

            if (toolDrawn)
            {
                sheathTimer -= Time.deltaTime;
                if (sheathTimer <= 0f)
                {
                    Sheath();
                }
            }

            if (interactAction == null || !interactAction.WasPerformedThisFrame())
            {
                return;
            }

            TryMine();
        }

        private void TryMine()
        {
            if (worldBootstrap == null || Camera.main == null || equipment == null)
            {
                return;
            }

            ToolDefinition tool = equipment.CurrentTool;
            if (tool == null || mineTimer > 0f)
            {
                return;
            }

            if (!TryRaycastTerrain(Camera.main.transform, out RaycastHit hit))
            {
                return;
            }

            VoxelWorld world = worldBootstrap.World;
            float voxelSize = worldBootstrap.Config.VoxelSize;

            // Le point d'impact tombe exactement sur la frontière entre deux voxels ; un
            // léger recul le long de la normale évite d'arrondir vers le voxel vide de
            // l'autre côté de cette frontière.
            Vector3 insidePoint = hit.point - (hit.normal * SurfaceEpsilon);
            int3 worldVoxel = WorldToVoxel(insidePoint, voxelSize);

            VoxelType voxelType = world.GetVoxel(worldVoxel).Type;
            ItemDefinition oreItem = ResolveOreItem(voxelType);
            if (oreItem == null)
            {
                return;
            }

            mineTimer = tool.MineCooldown;
            sheathTimer = ToolIdleSheathDelay;

            hasPendingTarget = true;
            pendingVoxel = worldVoxel;
            pendingVoxelType = voxelType;
            pendingHitPoint = hit.point;
            pendingOreItem = oreItem;

            int actionId = toolDrawn
                ? CombatActionId.PickaxeSwing
                : CombatActionId.PickaxeDrawSwing;
            toolDrawn = true;
            combatEvents?.TriggerAction(actionId);
        }

        // Appelé par CharacterCombatAnimationEvents.MineHit (AnimationEvent "OnMineHit" baké
        // dans le clip en cours) : c'est ICI, au moment du contact réel de l'animation, que le
        // voxel est retiré — si la cible a changé/disparu depuis TryMine (joueur qui a bougé
        // la caméra, ou voxel déjà miné par autre chose), le coup ne fait simplement rien.
        private void ApplyMine()
        {
            if (!hasPendingTarget || worldBootstrap == null)
            {
                return;
            }

            hasPendingTarget = false;

            VoxelWorld world = worldBootstrap.World;
            if (world.GetVoxel(pendingVoxel).Type != pendingVoxelType)
            {
                return;
            }

            dirtyChunksBuffer.Clear();
            if (!world.TrySetVoxel(pendingVoxel, Voxel.Air, dirtyChunksBuffer))
            {
                return;
            }

            foreach (int3 coord in dirtyChunksBuffer)
            {
                worldBootstrap.RequestRemesh(coord);
            }

            ItemPickup.Spawn(pendingOreItem, 1, pendingHitPoint);
        }

        private void Sheath()
        {
            toolDrawn = false;
            combatEvents?.TriggerAction(CombatActionId.PickaxeSheath);
        }

        private ItemDefinition ResolveOreItem(VoxelType type)
        {
            return type switch
            {
                VoxelType.OreCopper => copperOreItem,
                VoxelType.OreIron => ironOreItem,
                VoxelType.OreGold => goldOreItem,
                _ => null,
            };
        }

        // Visée depuis la caméra (le centre de l'écran reste la cible), mais portée comptée
        // depuis le joueur : la caméra 3e personne est plusieurs mètres derrière lui, donc le
        // rayon est allongé de cette distance, et le joueur lui-même (son CharacterController)
        // est ignoré. Seul le terrain (chunks enfants de WorldBootstrap) est minable.
        private bool TryRaycastTerrain(Transform look, out RaycastHit terrainHit)
        {
            terrainHit = default;

            Vector3 playerCenter = transform.position + Vector3.up * PlayerCenterHeight;
            float maxDistance = Vector3.Distance(look.position, playerCenter) + Reach;
            int count = Physics.RaycastNonAlloc(
                look.position,
                look.forward,
                raycastBuffer,
                maxDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            // RaycastNonAlloc ne trie pas ses résultats : on cherche le plus proche
            // qui n'appartient pas au joueur (l'obstacle réellement visé).
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = raycastBuffer[i];
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!found || hit.distance < terrainHit.distance)
                {
                    terrainHit = hit;
                    found = true;
                }
            }

            return found
                && terrainHit.collider.transform.IsChildOf(worldBootstrap.transform)
                && Vector3.Distance(terrainHit.point, playerCenter) <= Reach;
        }

        private static int3 WorldToVoxel(Vector3 worldPosition, float voxelSize)
        {
            return new int3(
                (int)math.floor(worldPosition.x / voxelSize),
                (int)math.floor(worldPosition.y / voxelSize),
                (int)math.floor(worldPosition.z / voxelSize)
            );
        }
    }
}
