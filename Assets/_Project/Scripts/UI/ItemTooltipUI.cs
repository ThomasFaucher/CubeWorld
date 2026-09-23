using System.Text;
using CubeWorld.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Tooltip inventaire : nom, catégorie, stats. Construit en runtime sous le Canvas.
    /// </summary>
    public sealed class ItemTooltipUI : MonoBehaviour
    {
        private static ItemTooltipUI instance;

        private RectTransform panel;
        private TMP_Text body;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private bool visible;

        public static void Show(ItemDefinition item, Vector2 screenPosition)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null || item == null)
            {
                return;
            }

            Ensure(canvas).ShowInternal(item, screenPosition);
        }

        public static void Hide()
        {
            if (instance != null)
            {
                instance.HideInternal();
            }
        }

        private static ItemTooltipUI Ensure(Canvas canvas)
        {
            if (instance != null)
            {
                return instance;
            }

            var go = new GameObject("ItemTooltipUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            instance = go.AddComponent<ItemTooltipUI>();
            instance.Build(canvas);
            return instance;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Build(Canvas canvas)
        {
            rootCanvas = canvas;

            panel = gameObject.GetComponent<RectTransform>();
            // Ancre au CENTRE du Canvas (comme InventoryDragSession.ghostRoot), pas à un coin :
            // ScreenPointToLocalPointInRectangle (voir PositionNear) renvoie un point relatif au
            // pivot du Canvas (son centre par défaut). Ancrer ce panneau à un coin désynchronisait
            // ce point du référentiel utilisé pour le positionner, collant le tooltip près du
            // coin haut-gauche de l'écran quasi indépendamment de la souris. Le pivot (0,1) reste
            // en haut-gauche DU PANNEAU LUI-MÊME, pour qu'il s'étende vers le bas-droite à partir
            // du point ancré (= près du curseur, voir l'offset (18,-18) dans PositionNear).
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);
            panel.sizeDelta = new Vector2(220f, 120f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f;

            Image bg = gameObject.AddComponent<Image>();
            Texture2D tex = Texture2D.whiteTexture;
            bg.sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            bg.color = InventoryUiTheme.WindowBg;
            bg.raycastTarget = false;

            var textGo = new GameObject("Body", typeof(RectTransform));
            textGo.transform.SetParent(transform, false);
            body = textGo.AddComponent<TextMeshProUGUI>();
            body.fontSize = 14;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = Color.white;
            body.raycastTarget = false;
            body.textWrappingMode = TextWrappingModes.Normal;
            RectTransform textRt = body.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 8f);
            textRt.offsetMax = new Vector2(-10f, -8f);
        }

        private void LateUpdate()
        {
            if (!visible)
            {
                return;
            }

            Vector2 screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
            PositionNear(screen);
        }

        private void ShowInternal(ItemDefinition item, Vector2 screenPosition)
        {
            body.text = BuildText(item);
            float height = Mathf.Clamp(40f + body.text.Split('\n').Length * 18f, 70f, 200f);
            panel.sizeDelta = new Vector2(220f, height);
            canvasGroup.alpha = 1f;
            visible = true;
            transform.SetAsLastSibling();
            PositionNear(screenPosition);
        }

        private void HideInternal()
        {
            visible = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void PositionNear(Vector2 screen)
        {
            var canvasRect = rootCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screen,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera,
                out Vector2 local
            );

            Vector2 desired = local + new Vector2(18f, -18f);

            // Clamp dans le rect du Canvas pour que le panneau (pivot haut-gauche, s'étend vers
            // le bas-droite) reste entièrement visible même quand la souris est près d'un bord —
            // sinon il continuerait à déborder de l'écran côté droit/bas.
            Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
            desired.x = Mathf.Clamp(desired.x, -halfCanvas.x, halfCanvas.x - panel.sizeDelta.x);
            desired.y = Mathf.Clamp(desired.y, -halfCanvas.y + panel.sizeDelta.y, halfCanvas.y);

            panel.anchoredPosition = desired;
        }

        private static string BuildText(ItemDefinition item)
        {
            var sb = new StringBuilder();
            sb.AppendLine(item.DisplayName);
            sb.AppendLine(CategoryLabel(item.Category));

            if (item.Category == ItemCategory.Weapon && item.Weapon != null)
            {
                WeaponDefinition w = item.Weapon;
                sb.AppendLine($"Dégâts : {w.Damage}");
                sb.AppendLine($"Portée : {w.Range:0.##}");
                sb.Append($"Cooldown : {w.AttackCooldown:0.##}s");
            }
            else if (item.Category == ItemCategory.Armor && item.Armor != null)
            {
                ArmorDefinition a = item.Armor;
                sb.AppendLine($"Emplacement : {ArmorSlotLabel(a.Slot)}");
                sb.AppendLine($"Défense : {a.Defense}");
                sb.Append($"PV bonus : {a.BonusMaxHP}");
            }
            else if (item.Category == ItemCategory.Tool && item.Tool != null)
            {
                sb.Append($"Cadence de minage : {item.Tool.MineCooldown:0.##}s");
            }
            else
            {
                sb.Append($"Stack max : {item.MaxStackSize}");
            }

            return sb.ToString();
        }

        private static string CategoryLabel(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Weapon => "Arme",
                ItemCategory.Armor => "Armure",
                ItemCategory.Tool => "Outil",
                ItemCategory.Material => "Matériau",
                _ => category.ToString(),
            };
        }

        private static string ArmorSlotLabel(ArmorSlot slot)
        {
            return slot switch
            {
                ArmorSlot.Head => "Tête",
                ArmorSlot.Chest => "Torse",
                ArmorSlot.Legs => "Jambes",
                _ => slot.ToString(),
            };
        }
    }
}
