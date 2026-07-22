using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Session de drag inventaire : stack tenu sous le curseur + ghost UI.
    /// Créé à la demande (Ensure) sous le Canvas.
    /// </summary>
    public sealed class InventoryDragSession : MonoBehaviour
    {
        private static InventoryDragSession instance;

        private PlayerInventory inventory;
        private PlayerEquipment equipment;
        private RectTransform ghostRoot;
        private Image ghostIcon;
        private TMP_Text ghostQty;
        private Canvas rootCanvas;
        private CanvasGroup ghostGroup;

        private ItemDefinition heldItem;
        private int heldQuantity;
        private ItemSlotRef? origin;
        private bool isDragging;

        public static bool IsDragging => instance != null && instance.isDragging;
        public static bool HasHeld => instance != null && instance.heldItem != null;

        public static InventoryDragSession Ensure(Canvas canvas)
        {
            if (instance != null)
            {
                return instance;
            }

            var go = new GameObject("InventoryDragSession", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            instance = go.AddComponent<InventoryDragSession>();
            instance.BuildGhost(canvas);
            instance.BindPlayer();
            return instance;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void BindPlayer()
        {
            Transform player = PlayerContext.Transform;
            if (player == null)
            {
                return;
            }

            inventory = player.GetComponent<PlayerInventory>();
            equipment = player.GetComponent<PlayerEquipment>();
        }

        private void BuildGhost(Canvas canvas)
        {
            rootCanvas = canvas;

            var ghostGo = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
            ghostGo.transform.SetParent(transform, false);
            ghostRoot = ghostGo.GetComponent<RectTransform>();
            ghostRoot.sizeDelta = new Vector2(48f, 48f);
            ghostRoot.pivot = new Vector2(0.5f, 0.5f);

            ghostGroup = ghostGo.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            ghostGroup.alpha = 0f;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(ghostRoot, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            ghostIcon = iconGo.AddComponent<Image>();
            ghostIcon.raycastTarget = false;
            ghostIcon.preserveAspect = true;

            var qtyGo = new GameObject("Qty", typeof(RectTransform));
            qtyGo.transform.SetParent(ghostRoot, false);
            ghostQty = qtyGo.AddComponent<TextMeshProUGUI>();
            ghostQty.fontSize = 14;
            ghostQty.alignment = TextAlignmentOptions.BottomRight;
            ghostQty.color = Color.white;
            ghostQty.raycastTarget = false;
            RectTransform qtyRt = ghostQty.rectTransform;
            qtyRt.anchorMin = Vector2.zero;
            qtyRt.anchorMax = Vector2.one;
            qtyRt.offsetMin = new Vector2(2f, 0f);
            qtyRt.offsetMax = new Vector2(-2f, -2f);

            transform.SetAsLastSibling();
        }

        private void LateUpdate()
        {
            if (heldItem == null || ghostRoot == null)
            {
                return;
            }

            Vector2 screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screen,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera,
                out Vector2 local
            );
            ghostRoot.anchoredPosition = local;
        }

        public bool BeginPick(ItemSlotRef source, int amount, bool asDragGesture)
        {
            if (inventory == null || equipment == null)
            {
                BindPlayer();
            }

            if (inventory == null || amount <= 0 || heldItem != null)
            {
                return false;
            }

            if (source.Kind == ItemSlotKind.Inventory)
            {
                if (!inventory.TryTakeFromSlot(
                        source.InventoryIndex,
                        amount,
                        out ItemDefinition item,
                        out int taken
                    )
                    || taken <= 0)
                {
                    return false;
                }

                heldItem = item;
                heldQuantity = taken;
                origin = source;
                ShowGhost();
                isDragging = asDragGesture;
                ItemTooltipUI.Hide();
                return true;
            }

            ItemDefinition cursorItem = null;
            int cursorQty = 0;
            if (!equipment.TryTakeEquippedToHeld(source.EquipmentKind, ref cursorItem, ref cursorQty))
            {
                return false;
            }

            heldItem = cursorItem;
            heldQuantity = cursorQty;
            origin = source;
            ShowGhost();
            isDragging = asDragGesture;
            ItemTooltipUI.Hide();
            return true;
        }

        public void UpdateDrag(PointerEventData eventData)
        {
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            ItemSlotInteractable target = FindSlotUnderPointer(eventData);
            bool dropped = false;

            if (target != null)
            {
                dropped = TryDropOn(target.SlotRef);
            }

            if (!dropped || heldItem != null)
            {
                CancelToOriginOrBag();
            }

            FinishHeldVisual();
            isDragging = false;
        }

        /// <summary>Dépôt par clic (stack déjà tenu, hors geste drag).</summary>
        public void ClickDeposit(ItemSlotRef target)
        {
            if (heldItem == null || isDragging)
            {
                return;
            }

            if (!TryDropOn(target) || heldItem != null)
            {
                CancelToOriginOrBag();
            }

            FinishHeldVisual();
        }

        public bool TryDropOn(ItemSlotRef target)
        {
            if (heldItem == null || heldQuantity <= 0 || inventory == null || equipment == null)
            {
                return false;
            }

            if (target.Kind == ItemSlotKind.Inventory)
            {
                bool ok = inventory.TryDepositHeld(
                    target.InventoryIndex,
                    ref heldItem,
                    ref heldQuantity
                );
                if (ok)
                {
                    RefreshGhost();
                }

                return ok;
            }

            bool equipped = equipment.TryEquipFromHeld(
                target.EquipmentKind,
                ref heldItem,
                ref heldQuantity
            );
            if (equipped)
            {
                RefreshGhost();
            }

            return equipped;
        }

        private void CancelToOriginOrBag()
        {
            if (heldItem == null || heldQuantity <= 0)
            {
                heldItem = null;
                heldQuantity = 0;
                origin = null;
                return;
            }

            if (origin.HasValue && origin.Value.Kind == ItemSlotKind.Inventory)
            {
                if (inventory.TryPlaceInSlot(
                        origin.Value.InventoryIndex,
                        heldItem,
                        heldQuantity,
                        out int leftover
                    ))
                {
                    heldQuantity = leftover;
                    if (heldQuantity <= 0)
                    {
                        heldItem = null;
                        origin = null;
                        return;
                    }
                }
            }

            if (origin.HasValue
                && origin.Value.Kind == ItemSlotKind.Equipment
                && heldQuantity >= 1
                && equipment.CanEquipInKind(heldItem, origin.Value.EquipmentKind))
            {
                ItemDefinition item = heldItem;
                int qty = heldQuantity;
                heldItem = null;
                heldQuantity = 0;
                if (equipment.TryEquipFromHeld(origin.Value.EquipmentKind, ref item, ref qty)
                    && item == null)
                {
                    origin = null;
                    return;
                }

                heldItem = item;
                heldQuantity = qty;
            }

            if (heldItem != null && inventory.TryAdd(heldItem, heldQuantity))
            {
                heldItem = null;
                heldQuantity = 0;
                origin = null;
                return;
            }

            Debug.LogWarning(
                "[CubeWorld] Drag cancel : impossible de remettre le stack (inventaire plein)."
            );
            heldItem = null;
            heldQuantity = 0;
            origin = null;
            inventory.NotifyContentsChanged();
        }

        private void ShowGhost()
        {
            if (ghostGroup == null)
            {
                return;
            }

            ItemIconDisplay.ApplyTo(ghostIcon, heldItem);
            ghostQty.text = heldQuantity > 1 ? heldQuantity.ToString() : string.Empty;
            ghostGroup.alpha = 0.9f;
            transform.SetAsLastSibling();
        }

        private void RefreshGhost()
        {
            if (heldItem == null)
            {
                FinishHeldVisual();
                return;
            }

            ShowGhost();
        }

        private void FinishHeldVisual()
        {
            if (heldItem != null)
            {
                ShowGhost();
                return;
            }

            if (ghostGroup != null)
            {
                ghostGroup.alpha = 0f;
            }

            if (ghostIcon != null)
            {
                ghostIcon.enabled = false;
            }

            if (ghostQty != null)
            {
                ghostQty.text = string.Empty;
            }

            origin = null;
        }

        private static ItemSlotInteractable FindSlotUnderPointer(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                ItemSlotInteractable slot =
                    result.gameObject.GetComponentInParent<ItemSlotInteractable>();
                if (slot != null)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
