using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Définition d'une pièce d'armure : bonus de PV et réduction de dégâts à
    /// plat. Pas de rendu visuel (stats uniquement pour cette phase).
    /// </summary>
    [CreateAssetMenu(fileName = "ArmorDefinition", menuName = "CubeWorld/Armor Definition")]
    public sealed class ArmorDefinition : ScriptableObject
    {
        [SerializeField]
        private string _displayName;

        [SerializeField]
        private ArmorSlot _slot;

        [SerializeField]
        private int _bonusMaxHP;

        [Tooltip("Réduction de dégâts à plat, appliquée dans PlayerHealth.ApplyDamage.")]
        [SerializeField]
        private int _defense;

        public string DisplayName => _displayName;
        public ArmorSlot Slot => _slot;
        public int BonusMaxHP => _bonusMaxHP;
        public int Defense => _defense;
    }
}
