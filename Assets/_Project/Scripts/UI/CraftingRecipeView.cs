using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>Vue d'une recette clonée depuis un template construit à la main (voir InventoryUI.RefreshRecipeList).</summary>
    public sealed class CraftingRecipeView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private Button _craftButton;

        public TMP_Text NameText => _nameText;
        public Button CraftButton => _craftButton;
    }
}
