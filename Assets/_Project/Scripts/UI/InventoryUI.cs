using System.Collections.Generic;
using CubeWorld.Combat;
using CubeWorld.Core;
using CubeWorld.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Panneau d'inventaire/équipement/crafting. Le layout (Canvas, panneaux,
    /// templates) est construit à la main dans l'éditeur ; ce composant clone
    /// les templates pour les listes dynamiques (slots, recettes) et met à
    /// jour les éléments fixes (emplacements d'équipement).
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionAsset _inputActions;

        [Header("Panneau")]
        [SerializeField]
        private GameObject _panelRoot;

        [Header("Slots d'inventaire")]
        [SerializeField]
        private Transform _slotGridParent;

        [SerializeField]
        private InventorySlotView _slotTemplate;

        [Header("Équipement")]
        [SerializeField]
        private Button _weaponSlotButton;

        [SerializeField]
        private TMP_Text _weaponSlotLabel;

        [SerializeField]
        private Button _headSlotButton;

        [SerializeField]
        private TMP_Text _headSlotLabel;

        [SerializeField]
        private Button _chestSlotButton;

        [SerializeField]
        private TMP_Text _chestSlotLabel;

        [SerializeField]
        private Button _legsSlotButton;

        [SerializeField]
        private TMP_Text _legsSlotLabel;

        [Header("Crafting")]
        [SerializeField]
        private Transform _recipeListParent;

        [SerializeField]
        private CraftingRecipeView _recipeTemplate;

        [SerializeField]
        private CraftingRecipeDefinition[] _availableRecipes;

        private readonly List<GameObject> spawnedSlots = new();
        private readonly List<GameObject> spawnedRecipes = new();

        private InputAction toggleAction;
        private PlayerInventory inventory;
        private PlayerEquipment equipment;
        private PlayerCrafting crafting;
        private bool isOpen;

        private void Start()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[CubeWorld] InventoryUI : _inputActions n'est pas assigné.");
                return;
            }

            if (_panelRoot == null)
            {
                Debug.LogError("[CubeWorld] InventoryUI : _panelRoot n'est pas assigné.");
                return;
            }

            Transform player = PlayerContext.Transform;
            if (player == null)
            {
                Debug.LogError(
                    "[CubeWorld] InventoryUI : PlayerContext.Transform n'est pas assigné (PlayerBootstrap doit s'exécuter avant)."
                );
                return;
            }

            inventory = player.GetComponent<PlayerInventory>();
            equipment = player.GetComponent<PlayerEquipment>();
            crafting = player.GetComponent<PlayerCrafting>();

            toggleAction = _inputActions.FindActionMap("Player").FindAction("ToggleInventory");
            if (toggleAction == null)
            {
                Debug.LogWarning(
                    "[CubeWorld] InventoryUI : action « ToggleInventory » introuvable dans l'action map Player."
                );
            }
            else
            {
                toggleAction.performed += OnTogglePerformed;
                toggleAction.Enable();
            }

            _slotTemplate.gameObject.SetActive(false);
            _recipeTemplate.gameObject.SetActive(false);
            SetOpen(false);

            WireEquipmentButton(_weaponSlotButton, () => equipment.UnequipWeapon());
            WireEquipmentButton(_headSlotButton, () => equipment.UnequipArmor(ArmorSlot.Head));
            WireEquipmentButton(_chestSlotButton, () => equipment.UnequipArmor(ArmorSlot.Chest));
            WireEquipmentButton(_legsSlotButton, () => equipment.UnequipArmor(ArmorSlot.Legs));
        }

        private void OnDestroy()
        {
            if (toggleAction != null)
            {
                toggleAction.performed -= OnTogglePerformed;
                toggleAction.Disable();
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            SetOpen(!isOpen);
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            _panelRoot.SetActive(open);

            if (open)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshEquipmentPanel();
            RefreshRecipeList();
        }

        private void RefreshSlots()
        {
            ClearSpawned(spawnedSlots);

            foreach (Inventory.Slot slot in inventory.Contents.Slots)
            {
                if (slot.Item == null)
                {
                    continue;
                }

                ItemDefinition item = slot.Item;
                InventorySlotView view = Instantiate(_slotTemplate, _slotGridParent);
                view.gameObject.SetActive(true);
                spawnedSlots.Add(view.gameObject);

                view.NameText.text = item.DisplayName;
                view.QuantityText.text = $"x{slot.Quantity}";
                view.Icon.color = item.Color;

                bool isEquippable =
                    item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Armor;
                view.EquipButton.gameObject.SetActive(isEquippable);

                if (!isEquippable)
                {
                    continue;
                }

                view.EquipButtonLabel.text = "Équiper";
                view.EquipButton.onClick.RemoveAllListeners();
                view.EquipButton.onClick.AddListener(() =>
                {
                    if (item.Category == ItemCategory.Weapon)
                    {
                        equipment.EquipWeapon(item);
                    }
                    else
                    {
                        equipment.EquipArmor(item);
                    }

                    RefreshAll();
                });
            }
        }

        private void RefreshEquipmentPanel()
        {
            SetSlotLabel(_weaponSlotLabel, equipment.WeaponItem);
            SetSlotLabel(_headSlotLabel, equipment.GetArmor(ArmorSlot.Head));
            SetSlotLabel(_chestSlotLabel, equipment.GetArmor(ArmorSlot.Chest));
            SetSlotLabel(_legsSlotLabel, equipment.GetArmor(ArmorSlot.Legs));
        }

        private void RefreshRecipeList()
        {
            ClearSpawned(spawnedRecipes);

            if (_availableRecipes == null)
            {
                return;
            }

            foreach (CraftingRecipeDefinition recipe in _availableRecipes)
            {
                if (recipe == null || recipe.OutputItem == null)
                {
                    continue;
                }

                CraftingRecipeView view = Instantiate(_recipeTemplate, _recipeListParent);
                view.gameObject.SetActive(true);
                spawnedRecipes.Add(view.gameObject);

                view.NameText.text = $"{recipe.OutputItem.DisplayName} x{recipe.OutputQuantity}";
                view.CraftButton.interactable = crafting.CanCraft(recipe);

                view.CraftButton.onClick.RemoveAllListeners();
                view.CraftButton.onClick.AddListener(() =>
                {
                    crafting.TryCraft(recipe);
                    RefreshAll();
                });
            }
        }

        private void WireEquipmentButton(Button button, System.Action unequipAction)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                unequipAction();
                RefreshAll();
            });
        }

        private static void SetSlotLabel(TMP_Text label, ItemDefinition item)
        {
            if (label != null)
            {
                label.text = item != null ? item.DisplayName : "Vide";
            }
        }

        private void ClearSpawned(List<GameObject> spawned)
        {
            foreach (GameObject go in spawned)
            {
                Destroy(go);
            }

            spawned.Clear();
        }
    }
}
