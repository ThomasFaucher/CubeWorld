using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Définition d'un outil équipable (ex. pioche). Data-driven comme WeaponDefinition :
    /// le minage (voir Player.PlayerMining) exige un ToolDefinition équipé, sa vitesse
    /// (MineCooldown) et son visuel (VisualKind) en dépendent.
    /// </summary>
    [CreateAssetMenu(fileName = "ToolDefinition", menuName = "CubeWorld/Tool Definition")]
    public sealed class ToolDefinition : ScriptableObject
    {
        [SerializeField]
        private string _displayName;

        [SerializeField]
        private float _mineCooldown = 0.9f;

        [Tooltip("Silhouette voxel affichée dans le dos du joueur quand cet outil est équipé (voir Player.CharacterModel.PlayerGearVisual).")]
        [SerializeField]
        private ToolVisualKind _visualKind = ToolVisualKind.Pickaxe;

        public string DisplayName => _displayName;
        public float MineCooldown => _mineCooldown;
        public ToolVisualKind VisualKind => _visualKind;
    }
}
