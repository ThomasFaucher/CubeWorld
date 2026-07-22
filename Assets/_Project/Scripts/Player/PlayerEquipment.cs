using System;
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

        public event Action EquipmentChanged;

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

        public ItemDefinition GetEquipped(EquipmentSlotKind kind)
        {
            return kind switch
            {
                EquipmentSlotKind.Weapon => weaponItem,
                EquipmentSlotKind.Head => headItem,
                EquipmentSlotKind.Chest => chestItem,
                EquipmentSlotKind.Legs => legsItem,
                _ => null,
            };
        }

        public bool EquipWeapon(ItemDefinition item)
        {
            if (item == null || item.Category != ItemCategory.Weapon || item.Weapon == null)
            {
                return false;
            }

            if (!inventory.TryRemove(item, 1))
            {
                return false;
            }

            if (weaponItem != null)
            {
                inventory.TryAdd(weaponItem, 1);
            }

            weaponItem = item;
            EquipmentChanged?.Invoke();
            return true;
        }

        public void UnequipWeapon()
        {
            if (weaponItem == null || !inventory.TryAdd(weaponItem, 1))
            {
                return;
            }

            weaponItem = null;
            EquipmentChanged?.Invoke();
        }

        public bool EquipArmor(ItemDefinition item)
        {
            if (item == null || item.Category != ItemCategory.Armor || item.Armor == null)
            {
                return false;
            }

            if (!inventory.TryRemove(item, 1))
            {
                return false;
            }

            ArmorSlot slot = item.Armor.Slot;
            ItemDefinition previous = GetArmor(slot);
            if (previous != null && !inventory.TryAdd(previous, 1))
            {
                inventory.TryAdd(item, 1);
                return false;
            }

            if (previous != null)
            {
                ApplyBonus(previous, -1);
            }

            SetArmor(slot, item);
            ApplyBonus(item, 1);
            EquipmentChanged?.Invoke();
            return true;
        }

        public void UnequipArmor(ArmorSlot slot)
        {
            ItemDefinition current = GetArmor(slot);
            if (current == null || !inventory.TryAdd(current, 1))
            {
                return;
            }

            SetArmor(slot, null);
            ApplyBonus(current, -1);
            EquipmentChanged?.Invoke();
        }

        /// <summary>
        /// Équipe 1 unité depuis un slot inventaire. L'ancienne pièce revient
        /// dans le même slot si possible, sinon via TryAdd.
        /// </summary>
        public bool TryEquipFromInventorySlot(int invIndex)
        {
            Inventory.Slot slot = inventory.GetSlot(invIndex);
            ItemDefinition item = slot.Item;
            if (item == null || slot.Quantity <= 0)
            {
                return false;
            }

            if (item.Category == ItemCategory.Weapon)
            {
                return TryEquipWeaponFromSlot(invIndex, item);
            }

            if (item.Category == ItemCategory.Armor)
            {
                return TryEquipArmorFromSlot(invIndex, item);
            }

            return false;
        }

        /// <summary>Déséquipe vers un slot inventaire (swap si le slot est occupé).</summary>
        public bool TryUnequipToInventorySlot(EquipmentSlotKind kind, int invIndex)
        {
            ItemDefinition equipped = GetEquipped(kind);
            if (equipped == null)
            {
                return false;
            }

            Inventory.Slot target = inventory.GetSlot(invIndex);

            if (target.Item == null)
            {
                if (!inventory.TryPlaceInSlot(invIndex, equipped, 1, out int leftover) || leftover > 0)
                {
                    return false;
                }

                ClearEquipped(kind);
                EquipmentChanged?.Invoke();
                return true;
            }

            // Slot occupé : n'accepte le swap que si l'item cible peut remplacer l'équipement.
            if (!CanEquipInKind(target.Item, kind))
            {
                return false;
            }

            if (!inventory.TryTakeFromSlot(invIndex, 1, out ItemDefinition taken, out int takenQty)
                || takenQty <= 0)
            {
                return false;
            }

            if (!inventory.TryPlaceInSlot(invIndex, equipped, 1, out int left) || left > 0)
            {
                // Rollback take.
                inventory.TryPlaceInSlot(invIndex, taken, takenQty, out _);
                return false;
            }

            // Remplacer l'équipement par l'item pris du slot.
            if (kind == EquipmentSlotKind.Weapon)
            {
                weaponItem = taken;
            }
            else
            {
                ArmorSlot armorSlot = ToArmorSlot(kind);
                ApplyBonus(equipped, -1);
                SetArmor(armorSlot, taken);
                ApplyBonus(taken, 1);
            }

            EquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Équipe depuis le curseur de drag (1 unité). Remet l'ancien dans
        /// l'inventaire ; leftover reste sur le curseur.
        /// </summary>
        public bool TryEquipFromHeld(
            EquipmentSlotKind kind,
            ref ItemDefinition heldItem,
            ref int heldQuantity
        )
        {
            if (heldItem == null || heldQuantity <= 0 || !CanEquipInKind(heldItem, kind))
            {
                return false;
            }

            ItemDefinition toEquip = heldItem;
            ItemDefinition previous = GetEquipped(kind);

            if (previous != null && !inventory.TryAdd(previous, 1))
            {
                return false;
            }

            if (kind == EquipmentSlotKind.Weapon)
            {
                weaponItem = toEquip;
            }
            else
            {
                ArmorSlot armorSlot = ToArmorSlot(kind);
                if (previous != null)
                {
                    ApplyBonus(previous, -1);
                }

                SetArmor(armorSlot, toEquip);
                ApplyBonus(toEquip, 1);
            }

            heldQuantity--;
            if (heldQuantity <= 0)
            {
                heldItem = null;
                heldQuantity = 0;
            }

            EquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>Prend l'équipement sur le curseur (déséquipe sans passer par l'inventaire).</summary>
        public bool TryTakeEquippedToHeld(
            EquipmentSlotKind kind,
            ref ItemDefinition heldItem,
            ref int heldQuantity
        )
        {
            ItemDefinition equipped = GetEquipped(kind);
            if (equipped == null)
            {
                return false;
            }

            if (heldItem != null)
            {
                // Curseur déjà occupé : tenter d'équiper le held à la place (swap).
                if (!CanEquipInKind(heldItem, kind))
                {
                    return false;
                }

                ItemDefinition previous = equipped;
                ItemDefinition incoming = heldItem;
                int incomingQty = heldQuantity;

                if (kind == EquipmentSlotKind.Weapon)
                {
                    weaponItem = incoming;
                }
                else
                {
                    ArmorSlot armorSlot = ToArmorSlot(kind);
                    ApplyBonus(previous, -1);
                    SetArmor(armorSlot, incoming);
                    ApplyBonus(incoming, 1);
                }

                // Une seule unité équipée ; le reste du stack held reste… mais
                // l'équipement n'accepte qu'1 : on ne devrait pick que qty 1.
                heldItem = previous;
                heldQuantity = 1;

                // Si incomingQty > 1, remettre le surplus dans l'inventaire.
                if (incomingQty > 1 && !inventory.TryAdd(incoming, incomingQty - 1))
                {
                    // Rollback impossible proprement : on laisse le surplus perdu
                    // évité en ne pickant qu'1 depuis un stack. Ici on force TryAdd.
                }

                EquipmentChanged?.Invoke();
                return true;
            }

            ClearEquipped(kind);
            heldItem = equipped;
            heldQuantity = 1;
            EquipmentChanged?.Invoke();
            return true;
        }

        public bool CanEquipInKind(ItemDefinition item, EquipmentSlotKind kind)
        {
            if (item == null)
            {
                return false;
            }

            return kind switch
            {
                EquipmentSlotKind.Weapon => item.Category == ItemCategory.Weapon
                    && item.Weapon != null,
                EquipmentSlotKind.Head => item.Category == ItemCategory.Armor
                    && item.Armor != null
                    && item.Armor.Slot == ArmorSlot.Head,
                EquipmentSlotKind.Chest => item.Category == ItemCategory.Armor
                    && item.Armor != null
                    && item.Armor.Slot == ArmorSlot.Chest,
                EquipmentSlotKind.Legs => item.Category == ItemCategory.Armor
                    && item.Armor != null
                    && item.Armor.Slot == ArmorSlot.Legs,
                _ => false,
            };
        }

        private bool TryEquipWeaponFromSlot(int invIndex, ItemDefinition item)
        {
            if (item.Weapon == null)
            {
                return false;
            }

            if (!inventory.TryTakeFromSlot(invIndex, 1, out _, out int taken) || taken <= 0)
            {
                return false;
            }

            ItemDefinition previous = weaponItem;
            weaponItem = item;

            if (previous != null)
            {
                if (!inventory.TryPlaceInSlot(invIndex, previous, 1, out int left) || left > 0)
                {
                    if (!inventory.TryAdd(previous, 1))
                    {
                        // Sac plein : remettre l'arme précédente et restaurer le slot.
                        weaponItem = previous;
                        inventory.TryPlaceInSlot(invIndex, item, 1, out _);
                        return false;
                    }
                }
            }

            EquipmentChanged?.Invoke();
            return true;
        }

        private bool TryEquipArmorFromSlot(int invIndex, ItemDefinition item)
        {
            if (item.Armor == null)
            {
                return false;
            }

            ArmorSlot armorSlot = item.Armor.Slot;
            EquipmentSlotKind kind = FromArmorSlot(armorSlot);
            if (!CanEquipInKind(item, kind))
            {
                return false;
            }

            if (!inventory.TryTakeFromSlot(invIndex, 1, out _, out int taken) || taken <= 0)
            {
                return false;
            }

            ItemDefinition previous = GetArmor(armorSlot);
            SetArmor(armorSlot, item);
            ApplyBonus(item, 1);

            if (previous != null)
            {
                ApplyBonus(previous, -1);
                if (!inventory.TryPlaceInSlot(invIndex, previous, 1, out int left) || left > 0)
                {
                    if (!inventory.TryAdd(previous, 1))
                    {
                        // Rollback.
                        SetArmor(armorSlot, previous);
                        ApplyBonus(item, -1);
                        ApplyBonus(previous, 1);
                        inventory.TryPlaceInSlot(invIndex, item, 1, out _);
                        return false;
                    }
                }
            }

            EquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Restaure l'équipement depuis une save (sans retirer de l'inventaire).
        /// Réapplique les bonus HP d'armure.
        /// </summary>
        public void RestoreEquipped(
            ItemDefinition weapon,
            ItemDefinition head,
            ItemDefinition chest,
            ItemDefinition legs
        )
        {
            if (headItem != null)
            {
                ApplyBonus(headItem, -1);
            }

            if (chestItem != null)
            {
                ApplyBonus(chestItem, -1);
            }

            if (legsItem != null)
            {
                ApplyBonus(legsItem, -1);
            }

            weaponItem = weapon != null && weapon.Category == ItemCategory.Weapon ? weapon : null;
            headItem = ValidArmor(head, ArmorSlot.Head);
            chestItem = ValidArmor(chest, ArmorSlot.Chest);
            legsItem = ValidArmor(legs, ArmorSlot.Legs);

            if (headItem != null)
            {
                ApplyBonus(headItem, 1);
            }

            if (chestItem != null)
            {
                ApplyBonus(chestItem, 1);
            }

            if (legsItem != null)
            {
                ApplyBonus(legsItem, 1);
            }

            EquipmentChanged?.Invoke();
        }

        private static ItemDefinition ValidArmor(ItemDefinition item, ArmorSlot slot)
        {
            if (item == null || item.Category != ItemCategory.Armor || item.Armor == null)
            {
                return null;
            }

            return item.Armor.Slot == slot ? item : null;
        }

        private void ClearEquipped(EquipmentSlotKind kind)
        {
            switch (kind)
            {
                case EquipmentSlotKind.Weapon:
                    weaponItem = null;
                    break;
                case EquipmentSlotKind.Head:
                case EquipmentSlotKind.Chest:
                case EquipmentSlotKind.Legs:
                    ItemDefinition current = GetEquipped(kind);
                    if (current != null)
                    {
                        ApplyBonus(current, -1);
                    }

                    SetArmor(ToArmorSlot(kind), null);
                    break;
            }
        }

        private static ArmorSlot ToArmorSlot(EquipmentSlotKind kind)
        {
            return kind switch
            {
                EquipmentSlotKind.Head => ArmorSlot.Head,
                EquipmentSlotKind.Chest => ArmorSlot.Chest,
                EquipmentSlotKind.Legs => ArmorSlot.Legs,
                _ => ArmorSlot.Head,
            };
        }

        private static EquipmentSlotKind FromArmorSlot(ArmorSlot slot)
        {
            return slot switch
            {
                ArmorSlot.Head => EquipmentSlotKind.Head,
                ArmorSlot.Chest => EquipmentSlotKind.Chest,
                ArmorSlot.Legs => EquipmentSlotKind.Legs,
                _ => EquipmentSlotKind.Head,
            };
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
            if (armorItem?.Armor == null || health == null)
            {
                return;
            }

            health.ApplyArmorBonus(armorItem.Armor.BonusMaxHP * sign);
        }

        private static int DefenseOf(ItemDefinition armorItem)
        {
            return armorItem != null && armorItem.Armor != null ? armorItem.Armor.Defense : 0;
        }
    }
}
