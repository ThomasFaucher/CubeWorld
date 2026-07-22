using CubeWorld.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>Ligne de craft runtime : icône résultat + nom + bouton.</summary>
    public sealed class CraftingRecipeView : MonoBehaviour
    {
        private Image icon;
        private TMP_Text nameText;
        private Button craftButton;
        private Image buttonImage;
        private TMP_Text buttonLabel;

        public TMP_Text NameText => nameText;
        public Button CraftButton => craftButton;

        public static CraftingRecipeView Create(Transform parent, Sprite uiSprite)
        {
            var go = new GameObject("RecipeRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 52f);

            Image bg = go.AddComponent<Image>();
            bg.sprite = uiSprite;
            bg.color = InventoryUiTheme.PanelInner;
            bg.raycastTarget = false;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredWidth = 40f;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(go.transform, false);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text name = nameGo.AddComponent<TextMeshProUGUI>();
            name.fontSize = 16;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.color = InventoryUiTheme.TextPrimary;
            name.raycastTarget = false;

            var btnGo = new GameObject("Craft", typeof(RectTransform));
            btnGo.transform.SetParent(go.transform, false);
            btnGo.AddComponent<LayoutElement>().preferredWidth = 96f;
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = uiSprite;
            btnImg.color = InventoryUiTheme.Button;
            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.transition = Selectable.Transition.ColorTint;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            TMP_Text label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "Craft";
            label.fontSize = 15;
            label.alignment = TextAlignmentOptions.Center;
            label.color = InventoryUiTheme.TextPrimary;
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var view = go.AddComponent<CraftingRecipeView>();
            view.icon = iconImg;
            view.nameText = name;
            view.craftButton = btn;
            view.buttonImage = btnImg;
            view.buttonLabel = label;
            return view;
        }

        public void Bind(CraftingRecipeDefinition recipe, bool canCraft)
        {
            if (recipe?.OutputItem == null)
            {
                return;
            }

            ItemIconDisplay.ApplyTo(icon, recipe.OutputItem);
            nameText.text = $"{recipe.OutputItem.DisplayName}  ×{recipe.OutputQuantity}";
            craftButton.interactable = canCraft;
            buttonImage.color = canCraft ? InventoryUiTheme.Button : InventoryUiTheme.ButtonDisabled;
            buttonLabel.color = canCraft
                ? InventoryUiTheme.TextPrimary
                : InventoryUiTheme.TextMuted;
        }
    }
}
