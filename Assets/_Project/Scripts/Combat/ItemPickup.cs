using CubeWorld.Core;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Objet ramassable dans le monde : cube coloré placeholder (pas d'icône/
    /// modèle encore) + trigger. Ramassé via l'action Interact du joueur (voir
    /// Player.PlayerLoot). Pas de stockage persistant : Collect() se contente
    /// de publier l'event, l'inventaire viendra en Phase 4.
    /// </summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        private ItemDefinition item;
        private int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        public static ItemPickup Spawn(ItemDefinition item, int quantity, Vector3 position)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = $"Pickup_{item.Id}";
            visual.transform.position = position;
            visual.transform.localScale = Vector3.one * 0.35f;

            Collider visualCollider = visual.GetComponent<Collider>();
            visualCollider.isTrigger = true;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                visual.GetComponent<Renderer>().sharedMaterial = new Material(shader)
                {
                    color = item.Color,
                };
            }

            ItemPickup pickup = visual.AddComponent<ItemPickup>();
            pickup.item = item;
            pickup.quantity = quantity;
            return pickup;
        }

        public void Collect()
        {
            EventBus.Publish(new ItemPickedUpEvent(item.Id, item.DisplayName, quantity));
            Destroy(gameObject);
        }
    }
}
