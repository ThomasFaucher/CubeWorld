using CubeWorld.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>Slot inventaire carré (icône + quantité), créé en runtime.</summary>
    public sealed class InventorySlotView : MonoBehaviour
    {
        private Image background;
        private Image border;
        private Image icon;
        private TMP_Text quantityText;
        private TMP_Text hotbarHint;

        public int SlotIndex { get; private set; } = -1;

        public static InventorySlotView Create(Transform parent, int index, Sprite uiSprite)
        {
            var go = new GameObject($"InvSlot_{index}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(InventoryUiTheme.SlotSize, InventoryUiTheme.SlotSize);

            Image bg = go.AddComponent<Image>();
            bg.sprite = uiSprite;
            bg.color = InventoryUiTheme.SlotEmpty;
            bg.raycastTarget = true;

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(go.transform, false);
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            Stretch(borderRt);
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = uiSprite;
            borderImg.color = InventoryUiTheme.SlotBorder;
            borderImg.raycastTarget = false;
            borderGo.transform.SetAsFirstSibling();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(48f, 48f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = null;
            iconImg.color = Color.clear;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            TMP_Text qty = CreateText(go.transform, "Qty", 15, TextAlignmentOptions.BottomRight);
            Stretch(qty.rectTransform);
            qty.rectTransform.offsetMin = new Vector2(4f, 2f);
            qty.rectTransform.offsetMax = new Vector2(-4f, -2f);

            TMP_Text hint = CreateText(go.transform, "Hint", 11, TextAlignmentOptions.TopLeft);
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(4f, 2f);
            hint.rectTransform.offsetMax = new Vector2(-4f, -2f);
            hint.color = InventoryUiTheme.TextMuted;

            var view = go.AddComponent<InventorySlotView>();
            view.background = bg;
            view.border = borderImg;
            view.icon = iconImg;
            view.quantityText = qty;
            view.hotbarHint = hint;
            view.BindIndex(index);
            return view;
        }

        public void BindIndex(int index)
        {
            SlotIndex = index;
            ItemSlotInteractable interactable = GetComponent<ItemSlotInteractable>();
            if (interactable == null)
            {
                interactable = gameObject.AddComponent<ItemSlotInteractable>();
            }

            interactable.ConfigureInventory(index);

            if (hotbarHint != null)
            {
                if (index < Player.PlayerInventory.HotbarSlotCount)
                {
                    hotbarHint.text = (index + 1).ToString();
                    hotbarHint.gameObject.SetActive(true);
                }
                else
                {
                    hotbarHint.text = string.Empty;
                    hotbarHint.gameObject.SetActive(false);
                }
            }
        }

        public void ApplyEmpty()
        {
            if (background != null)
            {
                background.color = InventoryUiTheme.SlotEmpty;
            }

            if (border != null)
            {
                border.color = InventoryUiTheme.SlotBorder;
            }

            if (icon != null)
            {
                icon.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.text = string.Empty;
            }
        }

        public void ApplyFilled(ItemDefinition item, int quantity)
        {
            if (background != null)
            {
                background.color = InventoryUiTheme.SlotFilled;
            }

            if (border != null)
            {
                border.color = InventoryUiTheme.AccentMuted;
            }

            ItemIconDisplay.ApplyTo(icon, item);

            if (quantityText != null)
            {
                quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
            }
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float size,
            TextAlignmentOptions align
        )
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.color = InventoryUiTheme.TextPrimary;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
