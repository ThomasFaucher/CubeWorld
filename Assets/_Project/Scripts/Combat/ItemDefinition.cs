using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Définition d'un objet ramassable. Pas d'icône/inventaire pour l'instant
    /// (Phase 4) : juste de quoi représenter l'objet dans le monde et l'identifier
    /// dans les events (voir ItemPickedUpEvent).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "CubeWorld/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip(
            "Identifiant stable de l'objet, utilisé dans les events (ne pas changer une fois utilisé)."
        )]
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [Tooltip("Couleur du cube placeholder représentant l'objet au sol.")]
        [SerializeField]
        private Color _color = Color.white;

        [Header("Inventaire")]
        [SerializeField]
        private ItemCategory _category = ItemCategory.Material;

        [SerializeField]
        private int _maxStackSize = 99;

        [Header("Équipement (si Category = Weapon/Armor)")]
        [Tooltip("Requis si Category = Weapon.")]
        [SerializeField]
        private WeaponDefinition _weapon;

        [Tooltip("Requis si Category = Armor.")]
        [SerializeField]
        private ArmorDefinition _armor;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Color Color => _color;
        public ItemCategory Category => _category;
        public int MaxStackSize => _maxStackSize;
        public WeaponDefinition Weapon => _weapon;
        public ArmorDefinition Armor => _armor;
    }
}
