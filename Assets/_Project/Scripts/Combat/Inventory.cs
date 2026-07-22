using System.Collections.Generic;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Inventaire à capacité fixe, empilement par ItemDefinition.MaxStackSize.
    /// Classe C# pure (comme MeleeAttackResolver) : composée par PlayerInventory.
    /// </summary>
    public sealed class Inventory
    {
        // Tableau (pas List<T>) : l'indexeur d'un tableau retourne une variable
        // adressable, donc slots[i].Quantity += n compile directement sur le struct.
        private readonly Slot[] slots;

        public Inventory(int capacity)
        {
            slots = new Slot[capacity];
        }

        public int Capacity => slots.Length;
        public IReadOnlyList<Slot> Slots => slots;

        public Slot GetSlot(int index)
        {
            if (!IsValidIndex(index))
            {
                return default;
            }

            return slots[index];
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = default;
            }
        }

        /// <summary>
        /// Écrit un slot (load). quantity &lt;= 0 ou item null → vide le slot.
        /// </summary>
        public bool SetSlot(int index, ItemDefinition item, int quantity)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            if (item == null || quantity <= 0)
            {
                slots[index] = default;
                return true;
            }

            slots[index].Item = item;
            slots[index].Quantity = System.Math.Min(quantity, item.MaxStackSize);
            return true;
        }

        /// <summary>
        /// Ajoute la quantité demandée, en empilant d'abord dans les slots
        /// existants puis en remplissant des slots vides. Pas d'ajout partiel :
        /// vérifie que tout rentre avant de modifier quoi que ce soit.
        /// </summary>
        public bool TryAdd(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            if (!HasRoomFor(item, quantity))
            {
                return false;
            }

            int remaining = quantity;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item != item || slots[i].Quantity >= item.MaxStackSize)
                {
                    continue;
                }

                int space = item.MaxStackSize - slots[i].Quantity;
                int added = System.Math.Min(space, remaining);
                slots[i].Quantity += added;
                remaining -= added;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item != null)
                {
                    continue;
                }

                int added = System.Math.Min(item.MaxStackSize, remaining);
                slots[i].Item = item;
                slots[i].Quantity = added;
                remaining -= added;
            }

            return true;
        }

        /// <summary>Retire la quantité demandée, ou rien du tout si elle n'est pas entièrement disponible.</summary>
        public bool TryRemove(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0 || CountOf(item) < quantity)
            {
                return false;
            }

            int remaining = quantity;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item != item)
                {
                    continue;
                }

                int removed = System.Math.Min(slots[i].Quantity, remaining);
                slots[i].Quantity -= removed;
                remaining -= removed;

                if (slots[i].Quantity <= 0)
                {
                    slots[i].Item = null;
                    slots[i].Quantity = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Retire jusqu'à <paramref name="amount"/> du slot. Retourne false si
        /// le slot est vide ou l'index invalide.
        /// </summary>
        public bool TryTakeFromSlot(
            int index,
            int amount,
            out ItemDefinition item,
            out int taken
        )
        {
            item = null;
            taken = 0;

            if (!IsValidIndex(index) || amount <= 0 || slots[index].Item == null)
            {
                return false;
            }

            taken = System.Math.Min(amount, slots[index].Quantity);
            item = slots[index].Item;
            slots[index].Quantity -= taken;

            if (slots[index].Quantity <= 0)
            {
                slots[index].Item = null;
                slots[index].Quantity = 0;
            }

            return true;
        }

        /// <summary>
        /// Place dans le slot : merge si même item, remplit un slot vide.
        /// Si le slot a un autre item, refuse (leftover = amount).
        /// </summary>
        public bool TryPlaceInSlot(
            int index,
            ItemDefinition item,
            int amount,
            out int leftover
        )
        {
            leftover = amount;

            if (!IsValidIndex(index) || item == null || amount <= 0)
            {
                return false;
            }

            Slot slot = slots[index];

            if (slot.Item == null)
            {
                int placed = System.Math.Min(item.MaxStackSize, amount);
                slots[index].Item = item;
                slots[index].Quantity = placed;
                leftover = amount - placed;
                return true;
            }

            if (slot.Item != item)
            {
                return false;
            }

            int space = item.MaxStackSize - slot.Quantity;
            if (space <= 0)
            {
                return false;
            }

            int merged = System.Math.Min(space, amount);
            slots[index].Quantity += merged;
            leftover = amount - merged;
            return true;
        }

        public bool TrySwapSlots(int a, int b)
        {
            if (!IsValidIndex(a) || !IsValidIndex(b) || a == b)
            {
                return false;
            }

            (slots[a], slots[b]) = (slots[b], slots[a]);
            return true;
        }

        /// <summary>
        /// Même item → merge (reste éventuel dans from). Sinon swap.
        /// </summary>
        public bool TryMergeOrSwap(int from, int to)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to) || from == to)
            {
                return false;
            }

            if (slots[from].Item == null)
            {
                return false;
            }

            if (slots[to].Item == null)
            {
                slots[to] = slots[from];
                slots[from] = default;
                return true;
            }

            if (slots[from].Item == slots[to].Item)
            {
                ItemDefinition item = slots[to].Item;
                int space = item.MaxStackSize - slots[to].Quantity;
                if (space <= 0)
                {
                    return false;
                }

                int moved = System.Math.Min(space, slots[from].Quantity);
                slots[to].Quantity += moved;
                slots[from].Quantity -= moved;
                if (slots[from].Quantity <= 0)
                {
                    slots[from] = default;
                }

                return true;
            }

            return TrySwapSlots(from, to);
        }

        /// <summary>
        /// Dépose le stack tenu : merge / place / swap. Met à jour held*.
        /// </summary>
        public bool TryDepositHeld(
            int index,
            ref ItemDefinition heldItem,
            ref int heldQuantity
        )
        {
            if (!IsValidIndex(index) || heldItem == null || heldQuantity <= 0)
            {
                return false;
            }

            Slot target = slots[index];

            if (target.Item == null || target.Item == heldItem)
            {
                if (!TryPlaceInSlot(index, heldItem, heldQuantity, out int leftover))
                {
                    return false;
                }

                heldQuantity = leftover;
                if (heldQuantity <= 0)
                {
                    heldItem = null;
                    heldQuantity = 0;
                }

                return true;
            }

            // Swap avec le contenu du slot.
            ItemDefinition swapItem = target.Item;
            int swapQty = target.Quantity;
            slots[index].Item = heldItem;
            slots[index].Quantity = heldQuantity;
            heldItem = swapItem;
            heldQuantity = swapQty;
            return true;
        }

        public int CountOf(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            int total = 0;
            foreach (Slot slot in slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        public bool Contains(ItemDefinition item, int quantity) => CountOf(item) >= quantity;

        private bool HasRoomFor(ItemDefinition item, int quantity)
        {
            int remaining = quantity;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item == item)
                {
                    remaining -= System.Math.Max(0, item.MaxStackSize - slots[i].Quantity);
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].Item == null)
                {
                    remaining -= item.MaxStackSize;
                }
            }

            return remaining <= 0;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < slots.Length;

        public struct Slot
        {
            public ItemDefinition Item;
            public int Quantity;
        }
    }
}
