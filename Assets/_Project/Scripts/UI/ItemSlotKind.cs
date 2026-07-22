using CubeWorld.Combat;

namespace CubeWorld.UI
{
    public enum ItemSlotKind
    {
        Inventory,
        Equipment,
    }

    /// <summary>Identifie une cible de drag & drop (slot inventaire/hotbar ou équipement).</summary>
    public readonly struct ItemSlotRef
    {
        public readonly ItemSlotKind Kind;
        public readonly int InventoryIndex;
        public readonly EquipmentSlotKind EquipmentKind;

        public static ItemSlotRef Inventory(int index) =>
            new(ItemSlotKind.Inventory, index, default);

        public static ItemSlotRef Equipment(EquipmentSlotKind equipmentKind) =>
            new(ItemSlotKind.Equipment, -1, equipmentKind);

        private ItemSlotRef(ItemSlotKind kind, int inventoryIndex, EquipmentSlotKind equipmentKind)
        {
            Kind = kind;
            InventoryIndex = inventoryIndex;
            EquipmentKind = equipmentKind;
        }
    }
}
