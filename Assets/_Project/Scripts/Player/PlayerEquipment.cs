using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Emplacements d'équipement du joueur (arme + 3 pièces d'armure). Les
    /// items équipés sont stockés comme ItemDefinition (pas WeaponDefinition/
    /// ArmorDefinition bruts) : un item déséquipé retourne directement dans
    /// PlayerInventory sans lookup inverse.
    /// </summary>
    public sealed class PlayerEquipment : MonoBehaviour
    {
        private PlayerInventory inventory;
        private PlayerHealth health;

        private ItemDefinition weaponItem;
        private ItemDefinition headItem;
        private ItemDefinition chestItem;
        private ItemDefinition legsItem;

        public WeaponDefinition CurrentWeapon => weaponItem != null ? weaponItem.Weapon : null;
        public ItemDefinition WeaponItem => weaponItem;

        public int TotalDefense => DefenseOf(headItem) + DefenseOf(chestItem) + DefenseOf(legsItem);

        public void Bind(PlayerInventory playerInventory, PlayerHealth playerHealth)
        {
            inventory = playerInventory;
            health = playerHealth;
        }

        public ItemDefinition GetArmor(ArmorSlot slot)
        {
            return slot switch
            {
                ArmorSlot.Head => headItem,
                ArmorSlot.Chest => chestItem,
                ArmorSlot.Legs => legsItem,
                _ => null,
            };
        }

        public bool EquipWeapon(ItemDefinition item)
        {
            if (item == null || item.Category != ItemCategory.Weapon || item.Weapon == null)
            {
                return false;
            }

            if (!inventory.Contents.TryRemove(item, 1))
            {
                return false;
            }

            if (weaponItem != null)
            {
                inventory.Contents.TryAdd(weaponItem, 1);
            }

            weaponItem = item;
            return true;
        }

        public void UnequipWeapon()
        {
            if (weaponItem == null || !inventory.Contents.TryAdd(weaponItem, 1))
            {
                return;
            }

            weaponItem = null;
        }

        public bool EquipArmor(ItemDefinition item)
        {
            if (item == null || item.Category != ItemCategory.Armor || item.Armor == null)
            {
                return false;
            }

            if (!inventory.Contents.TryRemove(item, 1))
            {
                return false;
            }

            ArmorSlot slot = item.Armor.Slot;
            ItemDefinition previous = GetArmor(slot);
            if (previous != null && inventory.Contents.TryAdd(previous, 1))
            {
                ApplyBonus(previous, -1);
            }

            SetArmor(slot, item);
            ApplyBonus(item, 1);
            return true;
        }

        public void UnequipArmor(ArmorSlot slot)
        {
            ItemDefinition current = GetArmor(slot);
            if (current == null || !inventory.Contents.TryAdd(current, 1))
            {
                return;
            }

            SetArmor(slot, null);
            ApplyBonus(current, -1);
        }

        private void SetArmor(ArmorSlot slot, ItemDefinition item)
        {
            switch (slot)
            {
                case ArmorSlot.Head:
                    headItem = item;
                    break;
                case ArmorSlot.Chest:
                    chestItem = item;
                    break;
                case ArmorSlot.Legs:
                    legsItem = item;
                    break;
            }
        }

        private void ApplyBonus(ItemDefinition armorItem, int sign)
        {
            health.ApplyArmorBonus(armorItem.Armor.BonusMaxHP * sign);
        }

        private static int DefenseOf(ItemDefinition armorItem)
        {
            return armorItem != null && armorItem.Armor != null ? armorItem.Armor.Defense : 0;
        }
    }
}
