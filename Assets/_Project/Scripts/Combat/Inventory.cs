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

        public struct Slot
        {
            public ItemDefinition Item;
            public int Quantity;
        }
    }
}
