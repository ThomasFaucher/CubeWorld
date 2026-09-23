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
    /// Inventaire reconstruit entièrement en runtime : overlay, fenêtre,
    /// paper-doll d'équipement, grille sac, craft.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionAsset _inputActions;

        [Header("Legacy scène (désactivé au runtime)")]
        [SerializeField]
        private GameObject _panelRoot;

        [Header("Crafting")]
        [SerializeField]
        private CraftingRecipeDefinition[] _availableRecipes;

        private readonly List<InventorySlotView> slotViews = new();
        private readonly List<CraftingRecipeView> recipeViews = new();

        private InputAction toggleAction;
        private PlayerInventory inventory;
        private PlayerEquipment equipment;
        private PlayerCrafting crafting;
        private bool isOpen;

        private GameObject runtimeRoot;
        private RectTransform recipeList;
        private TMP_Text statsText;
        private Sprite uiSprite;

        private EquipSlotUi weaponSlot;
        private EquipSlotUi toolSlot;
        private EquipSlotUi headSlot;
        private EquipSlotUi chestSlot;
        private EquipSlotUi legsSlot;

        public bool IsOpen => isOpen;

        private struct EquipSlotUi
        {
            public EquipmentSlotKind Kind;
            public Image Frame;
            public Image Icon;
            public TMP_Text Title;
            public TMP_Text Detail;
            public TMP_Text EmptyGlyph;
        }

        private void Start()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[CubeWorld] InventoryUI : _inputActions n'est pas assigné.");
                return;
            }

            Transform player = PlayerContext.Transform;
            if (player == null)
            {
                Debug.LogError(
                    "[CubeWorld] InventoryUI : PlayerContext.Transform manquant (PlayerBootstrap avant)."
                );
                return;
            }

            inventory = player.GetComponent<PlayerInventory>();
            equipment = player.GetComponent<PlayerEquipment>();
            crafting = player.GetComponent<PlayerCrafting>();

            // Ancien panneau scène : on le cache, tout est reconstruit.
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("[CubeWorld] InventoryUI : aucun Canvas.");
                return;
            }

            uiSprite = CreateWhiteSprite();
            BuildRuntimeUi(canvas);

            toggleAction = _inputActions.FindActionMap("Player").FindAction("ToggleInventory");
            if (toggleAction != null)
            {
                toggleAction.performed += OnTogglePerformed;
                toggleAction.Enable();
            }

            inventory.ContentsChanged += OnInventoryChanged;
            equipment.EquipmentChanged += OnInventoryChanged;

            InventoryDragSession.Ensure(canvas);
            PickupFeedbackUI.Ensure(canvas);

            if (GetComponent<HotbarUI>() == null)
            {
                gameObject.AddComponent<HotbarUI>();
            }

            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (toggleAction != null)
            {
                toggleAction.performed -= OnTogglePerformed;
                toggleAction.Disable();
            }

            if (inventory != null)
            {
                inventory.ContentsChanged -= OnInventoryChanged;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged()
        {
            if (isOpen)
            {
                RefreshAll();
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext context) => SetOpen(!isOpen);

        private void SetOpen(bool open)
        {
            isOpen = open;
            if (runtimeRoot != null)
            {
                runtimeRoot.SetActive(open);
            }

            if (open)
            {
                RefreshAll();
            }
            else
            {
                ItemTooltipUI.Hide();
            }
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshEquipmentPanel();
            RefreshRecipeList();
        }

        private void BuildRuntimeUi(Canvas canvas)
        {
            runtimeRoot = new GameObject("InventoryRuntime", typeof(RectTransform));
            runtimeRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = runtimeRoot.GetComponent<RectTransform>();
            Stretch(rootRt);

            // Fond assombri
            Image overlay = runtimeRoot.AddComponent<Image>();
            overlay.sprite = uiSprite;
            overlay.color = InventoryUiTheme.Overlay;
            overlay.raycastTarget = true;

            // Fenêtre centrale
            var window = CreatePanel(runtimeRoot.transform, "Window", InventoryUiTheme.WindowBg);
            RectTransform winRt = window.GetComponent<RectTransform>();
            winRt.anchorMin = winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(
                InventoryUiTheme.WindowWidth,
                InventoryUiTheme.WindowHeight
            );

            CreateTitle(window.transform, "Inventaire", new Vector2(0f, -18f));

            // Colonne gauche : paper-doll
            var doll = CreatePanel(window.transform, "EquipmentDoll", InventoryUiTheme.PanelInner);
            RectTransform dollRt = doll.GetComponent<RectTransform>();
            dollRt.anchorMin = new Vector2(0f, 0f);
            dollRt.anchorMax = new Vector2(0.38f, 1f);
            dollRt.offsetMin = new Vector2(18f, 18f);
            dollRt.offsetMax = new Vector2(-8f, -56f);

            TMP_Text equipLabel = CreateText(
                doll.transform,
                "EquipLabel",
                15,
                TextAlignmentOptions.Center
            );
            equipLabel.text = "ÉQUIPEMENT";
            equipLabel.color = InventoryUiTheme.TextMuted;
            RectTransform equipLabelRt = equipLabel.rectTransform;
            equipLabelRt.anchorMin = new Vector2(0f, 1f);
            equipLabelRt.anchorMax = new Vector2(1f, 1f);
            equipLabelRt.pivot = new Vector2(0.5f, 1f);
            equipLabelRt.anchoredPosition = new Vector2(0f, -12f);
            equipLabelRt.sizeDelta = new Vector2(-16f, 22f);

            headSlot = CreateEquipSlot(
                doll.transform,
                EquipmentSlotKind.Head,
                "Tête",
                "H",
                new Vector2(0.5f, 0.78f)
            );
            weaponSlot = CreateEquipSlot(
                doll.transform,
                EquipmentSlotKind.Weapon,
                "Arme",
                "W",
                new Vector2(0.22f, 0.48f)
            );
            toolSlot = CreateEquipSlot(
                doll.transform,
                EquipmentSlotKind.Tool,
                "Outil",
                "T",
                new Vector2(0.78f, 0.48f)
            );
            chestSlot = CreateEquipSlot(
                doll.transform,
                EquipmentSlotKind.Chest,
                "Torse",
                "C",
                new Vector2(0.5f, 0.48f)
            );
            legsSlot = CreateEquipSlot(
                doll.transform,
                EquipmentSlotKind.Legs,
                "Jambes",
                "L",
                new Vector2(0.5f, 0.18f)
            );

            // Silhouette décorative (cadre vertical)
            var silhouette = new GameObject("Silhouette", typeof(RectTransform));
            silhouette.transform.SetParent(doll.transform, false);
            silhouette.transform.SetAsFirstSibling();
            RectTransform silRt = silhouette.GetComponent<RectTransform>();
            silRt.anchorMin = new Vector2(0.5f, 0.12f);
            silRt.anchorMax = new Vector2(0.5f, 0.88f);
            silRt.pivot = new Vector2(0.5f, 0.5f);
            silRt.sizeDelta = new Vector2(56f, 0f);
            Image silImg = silhouette.AddComponent<Image>();
            silImg.sprite = uiSprite;
            silImg.color = new Color(1f, 1f, 1f, 0.04f);
            silImg.raycastTarget = false;

            statsText = CreateText(doll.transform, "Stats", 14, TextAlignmentOptions.Center);
            RectTransform statsRt = statsText.rectTransform;
            statsRt.anchorMin = new Vector2(0f, 0f);
            statsRt.anchorMax = new Vector2(1f, 0f);
            statsRt.pivot = new Vector2(0.5f, 0f);
            statsRt.anchoredPosition = new Vector2(0f, 10f);
            statsRt.sizeDelta = new Vector2(-16f, 28f);
            statsText.color = InventoryUiTheme.TextMuted;

            // Colonne droite : sac (haut) + craft (bas), sans chevauchement.
            var right = CreatePanel(window.transform, "BagColumn", InventoryUiTheme.PanelInner);
            RectTransform rightRt = right.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.38f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.offsetMin = new Vector2(8f, 18f);
            rightRt.offsetMax = new Vector2(-18f, -56f);

            var rightLayout = right.AddComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(14, 14, 12, 12);
            rightLayout.spacing = 8f;
            rightLayout.childAlignment = TextAnchor.UpperCenter;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;

            float slot = InventoryUiTheme.SlotSize;
            float gap = InventoryUiTheme.SlotGap;
            int hotbarCount = PlayerInventory.HotbarSlotCount;
            int capacity = inventory != null ? inventory.Capacity : 20;

            // Hotbar : une seule rangée de 9 (slots 0..8).
            TMP_Text hotbarLabel = CreateText(
                right.transform,
                "HotbarLabel",
                15,
                TextAlignmentOptions.Center
            );
            hotbarLabel.text = "HOTBAR";
            hotbarLabel.color = InventoryUiTheme.Accent;
            hotbarLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            Transform hotbarGrid = CreateSlotGrid(
                right.transform,
                "HotbarGrid",
                columns: hotbarCount,
                preferredHeight: slot,
                slot,
                gap
            );
            for (int i = 0; i < hotbarCount; i++)
            {
                slotViews.Add(InventorySlotView.Create(hotbarGrid, i, uiSprite));
            }

            // Sac : 9 colonnes comme la hotbar (capacity 27 → 2 rangées pleines).
            TMP_Text sacLabel = CreateText(
                right.transform,
                "SacLabel",
                15,
                TextAlignmentOptions.Center
            );
            sacLabel.text = "SAC";
            sacLabel.color = InventoryUiTheme.TextMuted;
            sacLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            int bagCols = hotbarCount;
            int bagCount = Mathf.Max(0, capacity - hotbarCount);
            int bagRows = Mathf.Max(1, Mathf.CeilToInt(bagCount / (float)bagCols));
            float bagHeight = (bagRows * slot) + (Mathf.Max(0, bagRows - 1) * gap);

            Transform bagGrid = CreateSlotGrid(
                right.transform,
                "BagGrid",
                columns: bagCols,
                preferredHeight: bagHeight,
                slot,
                gap
            );
            for (int i = hotbarCount; i < capacity; i++)
            {
                slotViews.Add(InventorySlotView.Create(bagGrid, i, uiSprite));
            }

            TMP_Text craftLabel = CreateText(
                right.transform,
                "CraftLabel",
                15,
                TextAlignmentOptions.Center
            );
            craftLabel.text = "ARTISANAT";
            craftLabel.color = InventoryUiTheme.TextMuted;
            craftLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var listGo = new GameObject("RecipeList", typeof(RectTransform));
            listGo.transform.SetParent(right.transform, false);
            recipeList = listGo.GetComponent<RectTransform>();
            var listLe = listGo.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1f;
            listLe.minHeight = 64f;
            listGo.AddComponent<RectMask2D>();

            var vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Hint bas de fenêtre
            TMP_Text hint = CreateText(window.transform, "Hint", 13, TextAlignmentOptions.Center);
            RectTransform hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 8f);
            hintRt.sizeDelta = new Vector2(-24f, 22f);
            hint.color = InventoryUiTheme.TextMuted;
            hint.text = "Glisser pour déplacer · Clic droit = moitié · Tab pour fermer";
        }

        private static Transform CreateSlotGrid(
            Transform parent,
            string name,
            int columns,
            float preferredHeight,
            float slot,
            float gap
        )
        {
            var gridGo = new GameObject(name, typeof(RectTransform));
            gridGo.transform.SetParent(parent, false);
            var gridLe = gridGo.AddComponent<LayoutElement>();
            gridLe.preferredHeight = preferredHeight;
            gridLe.minHeight = preferredHeight;
            gridLe.flexibleHeight = 0f;
            gridGo.AddComponent<RectMask2D>();

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(slot, slot);
            grid.spacing = new Vector2(gap, gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 0, 0);
            return gridGo.transform;
        }

        private EquipSlotUi CreateEquipSlot(
            Transform parent,
            EquipmentSlotKind kind,
            string title,
            string emptyGlyph,
            Vector2 anchor
        )
        {
            float size = InventoryUiTheme.EquipSlotSize;
            var go = new GameObject($"Equip_{kind}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size + 24f, size + 36f);

            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(go.transform, false);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 1f);
            frameRt.anchorMax = new Vector2(0.5f, 1f);
            frameRt.pivot = new Vector2(0.5f, 1f);
            frameRt.anchoredPosition = new Vector2(0f, -18f);
            frameRt.sizeDelta = new Vector2(size, size);

            Image frame = frameGo.AddComponent<Image>();
            frame.sprite = uiSprite;
            frame.color = InventoryUiTheme.EquipFrameEmpty;
            frame.raycastTarget = true;

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(frameGo.transform, false);
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            Stretch(borderRt);
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            Image border = borderGo.AddComponent<Image>();
            border.sprite = uiSprite;
            border.color = InventoryUiTheme.AccentMuted;
            border.raycastTarget = false;
            borderGo.transform.SetAsFirstSibling();

            var glyphGo = new GameObject("EmptyGlyph", typeof(RectTransform));
            glyphGo.transform.SetParent(frameGo.transform, false);
            TMP_Text glyph = glyphGo.AddComponent<TextMeshProUGUI>();
            glyph.text = emptyGlyph;
            glyph.fontSize = 28;
            glyph.fontStyle = FontStyles.Bold;
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.color = new Color(1f, 1f, 1f, 0.18f);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(frameGo.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(52f, 52f);
            Image icon = iconGo.AddComponent<Image>();
            icon.sprite = null;
            icon.color = Color.clear;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            TMP_Text titleTmp = CreateText(go.transform, "Title", 13, TextAlignmentOptions.Center);
            RectTransform titleRt = titleTmp.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0f, 16f);
            titleTmp.text = title;
            titleTmp.color = InventoryUiTheme.Accent;

            TMP_Text detail = CreateText(go.transform, "Detail", 12, TextAlignmentOptions.Center);
            RectTransform detailRt = detail.rectTransform;
            detailRt.anchorMin = new Vector2(0f, 0f);
            detailRt.anchorMax = new Vector2(1f, 0f);
            detailRt.pivot = new Vector2(0.5f, 0f);
            detailRt.anchoredPosition = Vector2.zero;
            detailRt.sizeDelta = new Vector2(0f, 16f);
            detail.color = InventoryUiTheme.TextMuted;
            detail.text = "—";

            ItemSlotInteractable interactable = frameGo.AddComponent<ItemSlotInteractable>();
            interactable.ConfigureEquipment(kind);

            return new EquipSlotUi
            {
                Kind = kind,
                Frame = frame,
                Icon = icon,
                Title = titleTmp,
                Detail = detail,
                EmptyGlyph = glyph,
            };
        }

        private void RefreshSlots()
        {
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Count; i++)
            {
                Inventory.Slot slot = inventory.GetSlot(i);
                if (slot.Item == null)
                {
                    slotViews[i].ApplyEmpty();
                }
                else
                {
                    slotViews[i].ApplyFilled(slot.Item, slot.Quantity);
                }
            }
        }

        private void RefreshEquipmentPanel()
        {
            if (equipment == null)
            {
                return;
            }

            ApplyEquipSlot(weaponSlot, equipment.WeaponItem);
            ApplyEquipSlot(toolSlot, equipment.ToolItem);
            ApplyEquipSlot(headSlot, equipment.GetArmor(ArmorSlot.Head));
            ApplyEquipSlot(chestSlot, equipment.GetArmor(ArmorSlot.Chest));
            ApplyEquipSlot(legsSlot, equipment.GetArmor(ArmorSlot.Legs));

            if (statsText != null)
            {
                int def = equipment.TotalDefense;
                ItemDefinition weapon = equipment.WeaponItem;
                string dmg = weapon?.Weapon != null ? weapon.Weapon.Damage.ToString() : "—";
                statsText.text = $"Dégâts {dmg}   ·   Défense {def}";
            }
        }

        private static void ApplyEquipSlot(EquipSlotUi slot, ItemDefinition item)
        {
            if (slot.Frame == null)
            {
                return;
            }

            bool empty = item == null;
            slot.Frame.color = empty
                ? InventoryUiTheme.EquipFrameEmpty
                : InventoryUiTheme.EquipFrame;
            slot.EmptyGlyph.enabled = empty;
            ItemIconDisplay.ApplyTo(slot.Icon, item);

            if (empty)
            {
                slot.Detail.text = "vide";
                slot.Detail.color = InventoryUiTheme.TextMuted;
                return;
            }

            slot.Detail.text = FormatEquipDetail(item);
            slot.Detail.color = InventoryUiTheme.TextPrimary;
        }

        private static string FormatEquipDetail(ItemDefinition item)
        {
            if (item.Category == ItemCategory.Weapon && item.Weapon != null)
            {
                return $"{item.DisplayName}  ·  {item.Weapon.Damage} dmg";
            }

            if (item.Category == ItemCategory.Armor && item.Armor != null)
            {
                return $"{item.DisplayName}  ·  +{item.Armor.Defense} def";
            }

            if (item.Category == ItemCategory.Tool && item.Tool != null)
            {
                return item.DisplayName;
            }

            return item.DisplayName;
        }

        private void RefreshRecipeList()
        {
            foreach (CraftingRecipeView view in recipeViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            recipeViews.Clear();

            if (_availableRecipes == null || crafting == null || recipeList == null)
            {
                return;
            }

            foreach (CraftingRecipeDefinition recipe in _availableRecipes)
            {
                if (recipe == null || recipe.OutputItem == null)
                {
                    continue;
                }

                CraftingRecipeView view = CraftingRecipeView.Create(recipeList, uiSprite);
                bool canCraft = crafting.CanCraft(recipe);
                view.Bind(recipe, canCraft);

                CraftingRecipeDefinition captured = recipe;
                view.CraftButton.onClick.RemoveAllListeners();
                view.CraftButton.onClick.AddListener(() =>
                {
                    crafting.TryCraft(captured);
                    RefreshAll();
                });

                recipeViews.Add(view);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = CreateWhiteSprite();
            img.color = color;
            img.raycastTarget = true;
            return go;
        }

        private static void CreateTitle(Transform parent, string text, Vector2 anchoredPos)
        {
            TMP_Text tmp = CreateText(parent, "Title", 26, TextAlignmentOptions.Center);
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = InventoryUiTheme.Accent;
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(-24f, 36f);
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float size,
            TextAlignmentOptions align
        )
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.color = InventoryUiTheme.TextPrimary;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
    }
}
