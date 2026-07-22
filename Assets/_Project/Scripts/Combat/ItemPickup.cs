using CubeWorld.Core;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Objet ramassable : billboard (icône) ou mini-cube teinté, avec bob.
    /// Ramassé via Player.PlayerLoot (Interact).
    /// </summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        private const float CubeScale = 0.35f;
        private const float BillboardScale = 0.5f;
        private const float GroundClearance = 0.55f;
        private const float BobAmplitude = 0.08f;
        private const float BobSpeed = 2.4f;

        private ItemDefinition item;
        private int quantity;
        private Transform visual;
        private Vector3 visualBaseLocal;
        private bool billboard;
        private float bobPhase;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        public static ItemPickup Spawn(ItemDefinition item, int quantity, Vector3 position)
        {
            var root = new GameObject($"Pickup_{item.Id}");
            root.transform.position = position + Vector3.up * GroundClearance;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;

            ItemPickup pickup = root.AddComponent<ItemPickup>();
            pickup.item = item;
            pickup.quantity = quantity;
            pickup.bobPhase = Random.Range(0f, Mathf.PI * 2f);
            pickup.BuildVisual(item);
            return pickup;
        }

        private void BuildVisual(ItemDefinition definition)
        {
            if (definition.Icon != null)
            {
                visual = CreateBillboard(definition);
                billboard = true;
            }
            else
            {
                visual = CreateCube(definition);
                billboard = false;
            }

            visualBaseLocal = visual.localPosition;
        }

        private Transform CreateBillboard(ItemDefinition definition)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Visual";
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = Vector3.one * BillboardScale;

            Collider col = quad.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader != null)
            {
                var mat = new Material(shader);
                Sprite icon = definition.Icon;
                mat.mainTexture = icon.texture;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", icon.texture);
                }

                mat.color = Color.white;
                // Transparence pour PNG.
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1f);
                }

                mat.EnableKeyword("_ALPHATEST_ON");
                if (mat.HasProperty("_Cutoff"))
                {
                    mat.SetFloat("_Cutoff", 0.1f);
                }

                quad.GetComponent<Renderer>().sharedMaterial = mat;
            }

            return quad.transform;
        }

        private Transform CreateCube(ItemDefinition definition)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * CubeScale;

            Collider col = cube.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = new Material(shader)
                {
                    color = definition.Color,
                };
            }

            return cube.transform;
        }

        private void LateUpdate()
        {
            if (visual == null)
            {
                return;
            }

            float bob = Mathf.Sin((Time.time * BobSpeed) + bobPhase) * BobAmplitude;
            visual.localPosition = visualBaseLocal + Vector3.up * bob;

            if (!billboard)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            visual.rotation = Quaternion.LookRotation(
                visual.position - cam.transform.position,
                Vector3.up
            );
        }

        public void Collect()
        {
            EventBus.Publish(new ItemPickedUpEvent(item.Id, item.DisplayName, quantity));
            Destroy(gameObject);
        }
    }
}
