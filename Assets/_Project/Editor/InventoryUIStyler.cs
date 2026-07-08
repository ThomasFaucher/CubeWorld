using CubeWorld.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CubeWorld.EditorTools
{
    /// <summary>
    /// Outil one-shot : corrige la hiérarchie et le style de l'UI d'inventaire
    /// construite à la main (panneaux mal imbriqués, tailles/couleurs par
    /// défaut). S'appuie sur les références déjà câblées dans InventoryUI
    /// plutôt que sur des noms d'objets, donc robuste à la hiérarchie actuelle
    /// (même si elle est mal rangée) : voir Restyle().
    /// </summary>
    public static class InventoryUIStyler
    {
        private const string BackgroundSpritePath = "UI/Skin/Background.psd";

        private static readonly Color PanelColor = new(0.06f, 0.07f, 0.11f, 0.94f);
        private static readonly Color SlotBgColor = new(1f, 1f, 1f, 0.06f);
        private static readonly Color TitleColor = new(1f, 0.82f, 0.35f, 1f);
        private static readonly Color EquipButtonColor = new(0.20f, 0.55f, 0.45f, 1f);
        private static readonly Color EquipmentSlotColor = new(0.14f, 0.16f, 0.22f, 1f);

        [MenuItem("CubeWorld/UI/Restyle Inventory UI")]
        public static void Restyle()
        {
            var inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
            if (inventoryUI == null)
            {
                Debug.LogError("[CubeWorld] Aucun InventoryUI trouvé dans la scène ouverte.");
                return;
            }

            var so = new SerializedObject(inventoryUI);

            RectTransform panelRoot = GetRect(so, "_panelRoot");
            RectTransform slotGridParent = GetRect(so, "_slotGridParent");
            RectTransform slotTemplate = GetRect(so, "_slotTemplate");
            RectTransform weaponButton = GetRect(so, "_weaponSlotButton");
            RectTransform headButton = GetRect(so, "_headSlotButton");
            RectTransform chestButton = GetRect(so, "_chestSlotButton");
            RectTransform legsButton = GetRect(so, "_legsSlotButton");
            RectTransform recipeListParent = GetRect(so, "_recipeListParent");
            RectTransform recipeTemplate = GetRect(so, "_recipeTemplate");

            if (panelRoot == null || slotGridParent == null || slotTemplate == null
                || weaponButton == null || headButton == null || chestButton == null || legsButton == null
                || recipeListParent == null || recipeTemplate == null)
            {
                Debug.LogError(
                    "[CubeWorld] InventoryUI a des champs non assignés dans l'Inspector : impossible de restyler tant qu'ils ne sont pas tous câblés."
                );
                return;
            }

            RectTransform equipmentPanel = weaponButton.parent.GetComponent<RectTransform>();
            RectTransform craftingPanel = recipeListParent.parent.GetComponent<RectTransform>();
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError("[CubeWorld] Impossible de trouver le Canvas parent de InventoryPanel.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Restyle Inventory UI");
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // --- 1. Regroupe les 3 panneaux sous un même conteneur plein écran,
            // pour qu'ils s'affichent/se cachent ensemble avec Tab, sans être
            // imbriqués les uns dans les autres (c'était le bug de mise en page).
            RectTransform uiRoot = FindOrCreateChild(canvasRect, "InventoryUIRoot");
            StretchFill(uiRoot, Vector2.zero, Vector2.zero);

            panelRoot.SetParent(uiRoot, false);
            equipmentPanel.SetParent(uiRoot, false);
            craftingPanel.SetParent(uiRoot, false);

            so.FindProperty("_panelRoot").objectReferenceValue = uiRoot.gameObject;
            so.ApplyModifiedProperties();

            // --- 2. Ancrage de chaque panneau sur une zone fixe de l'écran + fond stylé.
            StylePanelBackground(panelRoot, PanelColor);
            SetAnchors(panelRoot, new Vector2(0.03f, 0.08f), new Vector2(0.40f, 0.92f));

            StylePanelBackground(equipmentPanel, PanelColor);
            SetAnchors(equipmentPanel, new Vector2(0.45f, 0.55f), new Vector2(0.97f, 0.92f));

            StylePanelBackground(craftingPanel, PanelColor);
            SetAnchors(craftingPanel, new Vector2(0.45f, 0.08f), new Vector2(0.97f, 0.50f));

            EnsureTitle(panelRoot, "Inventaire");
            EnsureTitle(equipmentPanel, "Équipement");
            EnsureTitle(craftingPanel, "Craft");

            // --- 3. Grille d'inventaire.
            var grid = slotGridParent.GetComponent<GridLayoutGroup>()
                ?? slotGridParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.cellSize = new Vector2(140, 160);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;
            StretchFill(slotGridParent, new Vector2(16, 16), new Vector2(-16, -64));

            // --- 4. Template de slot (carte item).
            StyleSlotTemplate(slotTemplate);

            // --- 5. Panneau équipement : les 4 boutons empilés proprement.
            StyleEquipmentPanel(equipmentPanel, weaponButton, headButton, chestButton, legsButton);

            // --- 6. Panneau crafting.
            StretchFill(recipeListParent, new Vector2(14, 14), new Vector2(-14, -56));
            var vlg = recipeListParent.GetComponent<VerticalLayoutGroup>()
                ?? recipeListParent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            StyleRecipeTemplate(recipeTemplate);

            // --- 7. Les templates ne doivent jamais s'afficher tels quels.
            slotTemplate.gameObject.SetActive(false);
            recipeTemplate.gameObject.SetActive(false);

            // --- 8. EventSystem : s'assure que c'est bien le nouvel Input System.
            FixEventSystem();

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log("[CubeWorld] UI d'inventaire restylée. Pense à sauvegarder la scène (Ctrl+S).");
        }

        private static void StyleSlotTemplate(RectTransform slotTemplate)
        {
            var view = slotTemplate.GetComponent<InventorySlotView>();
            var so = new SerializedObject(view);

            RectTransform icon = GetRect(so, "_icon");
            RectTransform nameText = GetRect(so, "_nameText");
            RectTransform quantityText = GetRect(so, "_quantityText");
            RectTransform equipButton = GetRect(so, "_equipButton");

            slotTemplate.sizeDelta = new Vector2(140, 160);
            StylePanelBackground(slotTemplate, SlotBgColor);

            if (icon != null)
            {
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
                icon.pivot = new Vector2(0.5f, 1f);
                icon.anchoredPosition = new Vector2(0, -10);
                icon.sizeDelta = new Vector2(108, 108);
            }

            if (quantityText != null)
            {
                quantityText.anchorMin = quantityText.anchorMax = new Vector2(1f, 1f);
                quantityText.pivot = new Vector2(1f, 1f);
                quantityText.anchoredPosition = new Vector2(-6, -6);
                quantityText.sizeDelta = new Vector2(44, 22);
                SetFont(quantityText, 16, TextAlignmentOptions.TopRight, Color.white);
            }

            if (nameText != null)
            {
                nameText.anchorMin = new Vector2(0, 1);
                nameText.anchorMax = new Vector2(1, 1);
                nameText.pivot = new Vector2(0.5f, 1f);
                nameText.anchoredPosition = new Vector2(0, -122);
                nameText.sizeDelta = new Vector2(-10, 20);
                SetFont(nameText, 16, TextAlignmentOptions.Center, Color.white);
            }

            if (equipButton != null)
            {
                equipButton.anchorMin = new Vector2(0, 0);
                equipButton.anchorMax = new Vector2(1, 0);
                equipButton.pivot = new Vector2(0.5f, 0f);
                equipButton.anchoredPosition = new Vector2(0, 6);
                equipButton.sizeDelta = new Vector2(-16, 24);

                var equipImg = equipButton.GetComponent<Image>();
                if (equipImg != null)
                {
                    equipImg.color = EquipButtonColor;
                }
            }
        }

        private static void StyleEquipmentPanel(
            RectTransform panel,
            RectTransform weapon,
            RectTransform head,
            RectTransform chest,
            RectTransform legs
        )
        {
            var vlg = panel.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 64, 20);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            foreach (RectTransform button in new[] { weapon, head, chest, legs })
            {
                button.sizeDelta = new Vector2(button.sizeDelta.x, 52);

                var img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = EquipmentSlotColor;
                }

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontSize = 20;
                    label.color = Color.white;
                }
            }
        }

        private static void StyleRecipeTemplate(RectTransform recipeTemplate)
        {
            var view = recipeTemplate.GetComponent<CraftingRecipeView>();
            var so = new SerializedObject(view);

            RectTransform nameText = GetRect(so, "_nameText");
            RectTransform craftButton = GetRect(so, "_craftButton");

            recipeTemplate.sizeDelta = new Vector2(recipeTemplate.sizeDelta.x, 56);
            StylePanelBackground(recipeTemplate, SlotBgColor);

            var hlg = recipeTemplate.GetComponent<HorizontalLayoutGroup>()
                ?? recipeTemplate.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 8, 8);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            if (nameText != null)
            {
                var nameLayout = nameText.GetComponent<LayoutElement>() ?? nameText.gameObject.AddComponent<LayoutElement>();
                nameLayout.flexibleWidth = 1;
                SetFont(nameText, 18, TextAlignmentOptions.MidlineLeft, Color.white);
            }

            if (craftButton != null)
            {
                var buttonLayout = craftButton.GetComponent<LayoutElement>()
                    ?? craftButton.gameObject.AddComponent<LayoutElement>();
                buttonLayout.preferredWidth = 110;
                buttonLayout.flexibleWidth = 0;

                var img = craftButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = EquipButtonColor;
                }
            }
        }

        private static void FixEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Debug.Log("[CubeWorld] EventSystem créé avec Input System UI Input Module.");
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                return;
            }

            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void EnsureTitle(RectTransform panel, string text)
        {
            RectTransform rt = panel.Find("SectionTitle") as RectTransform;
            TextMeshProUGUI tmp;

            if (rt == null)
            {
                var go = new GameObject("SectionTitle", typeof(RectTransform));
                go.transform.SetParent(panel, false);
                rt = go.GetComponent<RectTransform>();
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = rt.GetComponent<TextMeshProUGUI>() ?? rt.gameObject.AddComponent<TextMeshProUGUI>();
            }

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(-20, 44);

            tmp.text = text;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = TitleColor;
            tmp.raycastTarget = false;
        }

        private static void StylePanelBackground(RectTransform rt, Color color)
        {
            var image = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BackgroundSpritePath);
            image.type = Image.Type.Sliced;
        }

        private static void SetFont(RectTransform rt, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var tmp = rt.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                return;
            }

            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
        }

        private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void StretchFill(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static RectTransform FindOrCreateChild(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform GetRect(SerializedObject so, string propertyName)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.objectReferenceValue == null)
            {
                return null;
            }

            return prop.objectReferenceValue switch
            {
                Component c => c.GetComponent<RectTransform>(),
                GameObject go => go.GetComponent<RectTransform>(),
                _ => null,
            };
        }
    }
}
