using System;
using CubeWorld.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Sélection parmi les 9 premiers slots de l'inventaire (hotbar).
    /// Molette souris + touches 1–9.
    /// </summary>
    public sealed class PlayerHotbar : MonoBehaviour
    {
        private PlayerInventory inventory;
        private int selectedIndex;

        public int SelectedIndex => selectedIndex;
        public int SlotCount => PlayerInventory.HotbarSlotCount;

        public Inventory.Slot SelectedSlot =>
            inventory != null ? inventory.GetHotbarSlot(selectedIndex) : default;

        public ItemDefinition SelectedItem => SelectedSlot.Item;

        public event Action SelectionChanged;

        public void Bind(PlayerInventory playerInventory)
        {
            inventory = playerInventory;
            selectedIndex = 0;
            SelectionChanged?.Invoke();
        }

        private void Update()
        {
            if (inventory == null)
            {
                return;
            }

            HandleScroll();
            HandleDigitKeys();
        }

        public void SelectIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, SlotCount - 1);
            selectedIndex = clamped;
            // Toujours notifier : permet à la hotbar de se réafficher même
            // si le joueur re-presse le slot déjà sélectionné.
            SelectionChanged?.Invoke();
        }

        public void CycleNext()
        {
            selectedIndex = (selectedIndex + 1) % SlotCount;
            SelectionChanged?.Invoke();
        }

        public void CyclePrevious()
        {
            selectedIndex = (selectedIndex - 1 + SlotCount) % SlotCount;
            SelectionChanged?.Invoke();
        }

        private void HandleScroll()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scrollY = mouse.scroll.ReadValue().y;
            if (scrollY > 0.1f)
            {
                CyclePrevious();
            }
            else if (scrollY < -0.1f)
            {
                CycleNext();
            }
        }

        private void HandleDigitKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) SelectIndex(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectIndex(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectIndex(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) SelectIndex(3);
            else if (keyboard.digit5Key.wasPressedThisFrame) SelectIndex(4);
            else if (keyboard.digit6Key.wasPressedThisFrame) SelectIndex(5);
            else if (keyboard.digit7Key.wasPressedThisFrame) SelectIndex(6);
            else if (keyboard.digit8Key.wasPressedThisFrame) SelectIndex(7);
            else if (keyboard.digit9Key.wasPressedThisFrame) SelectIndex(8);
        }
    }
}
