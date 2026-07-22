using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Hotbar des 9 premiers slots inventaire. Cachée par défaut ; slide vers
    /// le haut à la sélection (molette / 1–9), puis se range après idle.
    /// </summary>
    public sealed class HotbarUI : MonoBehaviour
    {
        private const float SlotSize = 64f;
        private const float SlotGap = 6f;
        private const float BottomMargin = 24f;
        private const float HiddenExtra = 16f;
        private const float SlideSpeed = 12f;
        private const float HideDelay = 2.2f;

        private static readonly Color SlotBg = InventoryUiTheme.SlotFilled;
        private static readonly Color SlotBgEmpty = InventoryUiTheme.SlotEmpty;
        private static readonly Color SlotSelected = InventoryUiTheme.Accent;
        private static readonly Color SlotIdleBorder = InventoryUiTheme.SlotBorder;
        private static readonly Color SlotEmptyBorder = new(1f, 1f, 1f, 0.06f);
        private static readonly Color SlotPulseBorder = new(0.55f, 1f, 0.65f, 0.95f);

        private PlayerInventory inventory;
        private PlayerHotbar hotbar;
        private InventoryUI inventoryUi;
        private RectTransform row;
        private HotbarSlotWidgets[] slots;
        private Vector2 shownPosition;
        private Vector2 hiddenPosition;
        private bool wantVisible;
        private float hideAtTime;
        private int[] lastHotbarQuantities;
        private float[] pulseUntil;

        private struct HotbarSlotWidgets
        {
            public Image Background;
            public Image Border;
            public Image Icon;
            public TMP_Text Quantity;
            public TMP_Text KeyLabel;
            public ItemSlotInteractable Interactable;
        }

        private void Start()
        {
            Transform player = PlayerContext.Transform;
            if (player == null)
            {
                Debug.LogError(
                    "[CubeWorld] HotbarUI : PlayerContext.Transform manquant (PlayerBootstrap avant)."
                );
                return;
            }

            inventory = player.GetComponent<PlayerInventory>();
            hotbar = player.GetComponent<PlayerHotbar>();
            inventoryUi = GetComponent<InventoryUI>();
            if (inventory == null || hotbar == null)
            {
                Debug.LogError("[CubeWorld] HotbarUI : PlayerInventory / PlayerHotbar manquants.");
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CubeWorld] HotbarUI : aucun Canvas dans la scène.");
                return;
            }

            BuildUi(canvas);
            InventoryDragSession.Ensure(canvas);
            PickupFeedbackUI.Ensure(canvas);
            lastHotbarQuantities = new int[PlayerInventory.HotbarSlotCount];
            pulseUntil = new float[PlayerInventory.HotbarSlotCount];
            inventory.ContentsChanged += OnContentsChanged;
            hotbar.SelectionChanged += OnSelectionChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.ContentsChanged -= OnContentsChanged;
            }

            if (hotbar != null)
            {
                hotbar.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void Update()
        {
            if (row == null)
            {
                return;
            }

            // Visible pendant drag, ou quand le panneau inventaire est ouvert.
            if (InventoryDragSession.HasHeld
                || InventoryDragSession.IsDragging
                || IsInventoryPanelOpen())
            {
                Reveal();
            }

            if (wantVisible && Time.unscaledTime >= hideAtTime)
            {
                wantVisible = false;
            }

            if (HasActivePulse())
            {
                Refresh();
            }

            Vector2 target = wantVisible ? shownPosition : hiddenPosition;
            row.anchoredPosition = Vector2.Lerp(
                row.anchoredPosition,
                target,
                1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime)
            );
        }

        private bool HasActivePulse()
        {
            if (pulseUntil == null)
            {
                return false;
            }

            float now = Time.unscaledTime;
            for (int i = 0; i < pulseUntil.Length; i++)
            {
                if (now < pulseUntil[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void OnSelectionChanged()
        {
            Refresh();
            Reveal();
        }

        private void OnContentsChanged()
        {
            if (slots != null && lastHotbarQuantities != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    Inventory.Slot slot = inventory.GetHotbarSlot(i);
                    int qty = slot.Item != null ? slot.Quantity : 0;
                    if (qty > lastHotbarQuantities[i])
                    {
                        pulseUntil[i] = Time.unscaledTime + 0.35f;
                        Reveal();
                    }

                    lastHotbarQuantities[i] = qty;
                }
            }

            Refresh();
        }

        private void Reveal()
        {
            wantVisible = true;
            hideAtTime = Time.unscaledTime + HideDelay;
        }

        private bool IsInventoryPanelOpen() => inventoryUi != null && inventoryUi.IsOpen;

        private void BuildUi(Canvas canvas)
        {
            var rootGo = new GameObject("Hotbar", typeof(RectTransform));
            rootGo.transform.SetParent(canvas.transform, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);

            float width =
                (SlotSize * PlayerInventory.HotbarSlotCount)
                + (SlotGap * (PlayerInventory.HotbarSlotCount - 1));
            root.sizeDelta = new Vector2(width, SlotSize);

            shownPosition = new Vector2(0f, BottomMargin);
            hiddenPosition = new Vector2(0f, -SlotSize - HiddenExtra);
            root.anchoredPosition = hiddenPosition;

            row = root;
            slots = new HotbarSlotWidgets[PlayerInventory.HotbarSlotCount];
            Sprite uiSprite = CreateWhiteSprite();

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateSlot(row, i, uiSprite);
            }
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        private static HotbarSlotWidgets CreateSlot(
            RectTransform parent,
            int index,
            Sprite uiSprite
        )
        {
            var slotGo = new GameObject($"HotbarSlot_{index + 1}", typeof(RectTransform));
            slotGo.transform.SetParent(parent, false);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0f, 0.5f);
            slotRt.anchorMax = new Vector2(0f, 0.5f);
            slotRt.pivot = new Vector2(0f, 0.5f);
            slotRt.sizeDelta = new Vector2(SlotSize, SlotSize);
            slotRt.anchoredPosition = new Vector2(index * (SlotSize + SlotGap), 0f);

            Image bg = slotGo.AddComponent<Image>();
            bg.sprite = uiSprite;
            bg.color = SlotBg;
            bg.raycastTarget = true;

            ItemSlotInteractable interactable = slotGo.AddComponent<ItemSlotInteractable>();
            interactable.ConfigureInventory(index);

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(slotGo.transform, false);
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            Stretch(borderRt);
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            Image border = borderGo.AddComponent<Image>();
            border.sprite = uiSprite;
            border.color = SlotIdleBorder;
            border.raycastTarget = false;
            borderGo.transform.SetAsFirstSibling();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(slotGo.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            Image icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
            icon.raycastTarget = false;

            TMP_Text qty = CreateLabel(
                slotGo.transform,
                "Qty",
                14,
                TextAlignmentOptions.BottomRight
            );
            RectTransform qtyRt = qty.rectTransform;
            qtyRt.anchorMin = new Vector2(0f, 0f);
            qtyRt.anchorMax = new Vector2(1f, 1f);
            qtyRt.offsetMin = new Vector2(4f, 2f);
            qtyRt.offsetMax = new Vector2(-4f, -2f);

            TMP_Text key = CreateLabel(slotGo.transform, "Key", 12, TextAlignmentOptions.TopLeft);
            RectTransform keyRt = key.rectTransform;
            keyRt.anchorMin = new Vector2(0f, 0f);
            keyRt.anchorMax = new Vector2(1f, 1f);
            keyRt.offsetMin = new Vector2(4f, 2f);
            keyRt.offsetMax = new Vector2(-4f, -2f);
            key.text = (index + 1).ToString();
            key.color = new Color(1f, 1f, 1f, 0.55f);

            return new HotbarSlotWidgets
            {
                Background = bg,
                Border = border,
                Icon = icon,
                Quantity = qty,
                KeyLabel = key,
                Interactable = interactable,
            };
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            int fontSize,
            TextAlignmentOptions align
        )
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
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

        private void Refresh()
        {
            if (slots == null || inventory == null || hotbar == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                HotbarSlotWidgets w = slots[i];
                Inventory.Slot slot = inventory.GetHotbarSlot(i);
                bool selected = i == hotbar.SelectedIndex;
                bool empty = slot.Item == null;
                bool pulsing = pulseUntil != null && Time.unscaledTime < pulseUntil[i];

                if (pulsing)
                {
                    w.Border.color = SlotPulseBorder;
                }
                else if (selected)
                {
                    w.Border.color = SlotSelected;
                }
                else
                {
                    w.Border.color = empty ? SlotEmptyBorder : SlotIdleBorder;
                }

                w.Background.color = selected
                    ? new Color(0.12f, 0.11f, 0.08f, 0.92f)
                    : empty
                        ? SlotBgEmpty
                        : SlotBg;

                if (empty)
                {
                    w.Icon.enabled = false;
                    w.Quantity.text = string.Empty;
                    continue;
                }

                ItemIconDisplay.ApplyTo(w.Icon, slot.Item);
                w.Quantity.text = slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
            }
        }
    }
}
