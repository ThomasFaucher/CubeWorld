using CubeWorld.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Ramassage d'objets au sol : sur l'action « Interact » (interaction Hold
    /// de l'asset), cherche le pickup le plus proche et le collecte.
    /// </summary>
    public sealed class PlayerLoot : MonoBehaviour
    {
        [SerializeField]
        private float _pickupRadius = 2f;

        private InputAction interactAction;
        private PlayerInventory inventory;

        public void BindInput(InputActionAsset inputActions, PlayerInventory playerInventory)
        {
            interactAction = inputActions.FindActionMap("Player").FindAction("Interact");
            inventory = playerInventory;
        }

        private void Update()
        {
            if (interactAction == null || !interactAction.WasPerformedThisFrame())
            {
                return;
            }

            ItemPickup nearest = FindNearestPickup();
            if (nearest == null)
            {
                return;
            }

            // Inventaire plein : le pickup reste au sol, rien n'est perdu.
            if (inventory.Contents.TryAdd(nearest.Item, nearest.Quantity))
            {
                nearest.Collect();
            }
        }

        private ItemPickup FindNearestPickup()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                _pickupRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            ItemPickup nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                ItemPickup pickup = hit.GetComponent<ItemPickup>();
                if (pickup == null)
                {
                    continue;
                }

                float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = pickup;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }
    }
}
