using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeWorld.UI
{
    /// <summary>
    /// Slot inventaire / hotbar / équipement : drag LMB (stack entier),
    /// clic droit = moitié, hover = tooltip.
    /// </summary>
    public sealed class ItemSlotInteractable
        : MonoBehaviour,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IPointerClickHandler,
            IPointerEnterHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private ItemSlotKind _slotKind = ItemSlotKind.Inventory;

        [SerializeField]
        private int _inventoryIndex;

        [SerializeField]
        private EquipmentSlotKind _equipmentKind;

        private Canvas canvas;
        private bool suppressClick;

        public ItemSlotRef SlotRef =>
            _slotKind == ItemSlotKind.Inventory
                ? ItemSlotRef.Inventory(_inventoryIndex)
                : ItemSlotRef.Equipment(_equipmentKind);

        public void ConfigureInventory(int index)
        {
            _slotKind = ItemSlotKind.Inventory;
            _inventoryIndex = index;
        }

        public void ConfigureEquipment(EquipmentSlotKind kind)
        {
            _slotKind = ItemSlotKind.Equipment;
            _equipmentKind = kind;
        }

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (InventoryDragSession.HasHeld)
            {
                return;
            }

            Canvas c = ResolveCanvas();
            if (c == null)
            {
                return;
            }

            int amount = GetAvailableQuantity();
            if (amount <= 0)
            {
                return;
            }

            InventoryDragSession session = InventoryDragSession.Ensure(c);
            if (session.BeginPick(SlotRef, amount, asDragGesture: true))
            {
                suppressClick = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Canvas c = ResolveCanvas();
            if (c == null)
            {
                return;
            }

            InventoryDragSession.Ensure(c).UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Canvas c = ResolveCanvas();
            if (c == null)
            {
                return;
            }

            InventoryDragSession.Ensure(c).EndDrag(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressClick)
            {
                suppressClick = false;
                return;
            }

            Canvas c = ResolveCanvas();
            if (c == null)
            {
                return;
            }

            InventoryDragSession session = InventoryDragSession.Ensure(c);

            if (eventData.button == PointerEventData.InputButton.Left
                && InventoryDragSession.HasHeld
                && !InventoryDragSession.IsDragging)
            {
                session.ClickDeposit(SlotRef);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            if (InventoryDragSession.IsDragging || InventoryDragSession.HasHeld)
            {
                return;
            }

            int available = GetAvailableQuantity();
            if (available <= 0)
            {
                return;
            }

            int half = Mathf.CeilToInt(available * 0.5f);
            session.BeginPick(SlotRef, half, asDragGesture: false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (InventoryDragSession.IsDragging || InventoryDragSession.HasHeld)
            {
                return;
            }

            ItemDefinition item = PeekItem();
            if (item == null)
            {
                return;
            }

            ItemTooltipUI.Show(item, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Hide();
        }

        private Canvas ResolveCanvas()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            return canvas != null ? canvas : FindFirstObjectByType<Canvas>();
        }

        private int GetAvailableQuantity()
        {
            if (_slotKind == ItemSlotKind.Inventory)
            {
                PlayerInventory inv = GetInventory();
                if (inv == null)
                {
                    return 0;
                }

                Inventory.Slot slot = inv.GetSlot(_inventoryIndex);
                return slot.Item != null ? slot.Quantity : 0;
            }

            PlayerEquipment eq = GetEquipment();
            if (eq == null)
            {
                return 0;
            }

            return eq.GetEquipped(_equipmentKind) != null ? 1 : 0;
        }

        private ItemDefinition PeekItem()
        {
            if (_slotKind == ItemSlotKind.Inventory)
            {
                PlayerInventory inv = GetInventory();
                return inv != null ? inv.GetSlot(_inventoryIndex).Item : null;
            }

            PlayerEquipment eq = GetEquipment();
            return eq != null ? eq.GetEquipped(_equipmentKind) : null;
        }

        private static PlayerInventory GetInventory()
        {
            Transform player = PlayerContext.Transform;
            return player != null ? player.GetComponent<PlayerInventory>() : null;
        }

        private static PlayerEquipment GetEquipment()
        {
            Transform player = PlayerContext.Transform;
            return player != null ? player.GetComponent<PlayerEquipment>() : null;
        }
    }
}
