using System;
using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>Possède l'Inventory du joueur (capacité fixe, voir Combat.Inventory).</summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int HotbarSlotCount = 9;

        // Hotbar (9) + 2 rangées de sac (18) = grille pleine, sans trous.
        [SerializeField]
        private int _capacity = 27;

        public Inventory Contents { get; private set; }

        /// <summary>Notifié après toute mutation réussie (loot, équipement, craft…).</summary>
        public event Action ContentsChanged;

        private void Awake()
        {
            // Capacité min = hotbar + un peu de sac.
            int capacity = Mathf.Max(_capacity, HotbarSlotCount);
            Contents = new Inventory(capacity);
        }

        public int Capacity => Contents != null ? Contents.Capacity : 0;

        public Inventory.Slot GetSlot(int index) =>
            Contents != null ? Contents.GetSlot(index) : default;

        public bool TryAdd(ItemDefinition item, int quantity)
        {
            if (!Contents.TryAdd(item, quantity))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TryRemove(ItemDefinition item, int quantity)
        {
            if (!Contents.TryRemove(item, quantity))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TryTakeFromSlot(
            int index,
            int amount,
            out ItemDefinition item,
            out int taken
        )
        {
            if (!Contents.TryTakeFromSlot(index, amount, out item, out taken))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TryPlaceInSlot(
            int index,
            ItemDefinition item,
            int amount,
            out int leftover
        )
        {
            if (!Contents.TryPlaceInSlot(index, item, amount, out leftover))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TrySwapSlots(int a, int b)
        {
            if (!Contents.TrySwapSlots(a, b))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TryMergeOrSwap(int from, int to)
        {
            if (!Contents.TryMergeOrSwap(from, to))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public bool TryDepositHeld(
            int index,
            ref ItemDefinition heldItem,
            ref int heldQuantity
        )
        {
            if (!Contents.TryDepositHeld(index, ref heldItem, ref heldQuantity))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        public void NotifyContentsChanged() => ContentsChanged?.Invoke();

        public void ClearSlots()
        {
            Contents.Clear();
            ContentsChanged?.Invoke();
        }

        public bool SetSlot(int index, ItemDefinition item, int quantity)
        {
            if (!Contents.SetSlot(index, item, quantity))
            {
                return false;
            }

            ContentsChanged?.Invoke();
            return true;
        }

        /// <summary>Remplace tout le contenu sans notifier à chaque slot (un seul event à la fin).</summary>
        public void ReplaceAllSlots(Inventory.Slot[] source)
        {
            Contents.Clear();
            if (source != null)
            {
                int count = Mathf.Min(source.Length, Contents.Capacity);
                for (int i = 0; i < count; i++)
                {
                    Contents.SetSlot(i, source[i].Item, source[i].Quantity);
                }
            }

            ContentsChanged?.Invoke();
        }

        public Inventory.Slot GetHotbarSlot(int index)
        {
            if (index < 0 || index >= HotbarSlotCount || Contents == null)
            {
                return default;
            }

            return Contents.GetSlot(index);
        }
    }
}
