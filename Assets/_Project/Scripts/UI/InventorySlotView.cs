using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Vue d'un slot d'inventaire. Le template (racine + ces références) est
    /// construit à la main dans l'éditeur ; InventoryUI clone ce composant par
    /// slot occupé (voir InventoryUI.RefreshSlots).
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _quantityText;

        [SerializeField]
        private Button _equipButton;

        [SerializeField]
        private TMP_Text _equipButtonLabel;

        public Image Icon => _icon;
        public TMP_Text NameText => _nameText;
        public TMP_Text QuantityText => _quantityText;
        public Button EquipButton => _equipButton;
        public TMP_Text EquipButtonLabel => _equipButtonLabel;
    }
}
