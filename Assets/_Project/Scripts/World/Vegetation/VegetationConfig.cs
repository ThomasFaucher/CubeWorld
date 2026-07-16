using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Configuration de la végétation procédurale, éditable dans l'inspecteur
    /// sans toucher au code — même pattern que <see cref="WorldConfig"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "VegetationConfig", menuName = "CubeWorld/Vegetation Config")]
    public sealed class VegetationConfig : ScriptableObject
    {
        [Header("Touffes d'herbe")]
        [Tooltip("Probabilité qu'une colonne d'herbe exposée porte une touffe.")]
        [SerializeField]
        private float _grassTuftDensity = 0.3f;

        [Tooltip("Hauteur minimale d'un brin d'herbe, en unités voxel.")]
        [SerializeField]
        private float _grassTuftMinSize = 0.3f;

        [Tooltip("Hauteur maximale d'un brin d'herbe, en unités voxel.")]
        [SerializeField]
        private float _grassTuftMaxSize = 0.6f;

        [Tooltip("Largeur minimale d'un brin (quad croisé), en unités voxel.")]
        [SerializeField]
        private float _grassTuftMinWidth = 0.025f;

        [Tooltip("Largeur maximale d'un brin (quad croisé), en unités voxel.")]
        [SerializeField]
        private float _grassTuftMaxWidth = 0.045f;

        [Header("Fleurs (Plaines)")]
        [Tooltip("Probabilité qu'une colonne Grass en biome Plaines porte des fleurs.")]
        [SerializeField]
        private float _flowerDensity = 0.12f;

        [Tooltip("Hauteur minimale d'un brin de fleur, en unités voxel.")]
        [SerializeField]
        private float _flowerMinSize = 0.28f;

        [Tooltip("Hauteur maximale d'un brin de fleur, en unités voxel.")]
        [SerializeField]
        private float _flowerMaxSize = 0.5f;

        [Tooltip("Largeur minimale d'un brin de fleur, en unités voxel.")]
        [SerializeField]
        private float _flowerMinWidth = 0.03f;

        [Tooltip("Largeur maximale d'un brin de fleur, en unités voxel.")]
        [SerializeField]
        private float _flowerMaxWidth = 0.055f;

        [Header("Arbres (Forêt)")]
        [Tooltip("Probabilité qu'une colonne Grass en biome Forêt porte un arbre.")]
        [SerializeField]
        private float _treeDensity = 0.012f;

        [Tooltip("Distance minimale (en voxels) entre deux arbres acceptés dans le même chunk.")]
        [SerializeField]
        private int _treeMinSpacing = 7;

        [Tooltip(
            "Nombre de meshes d'arbre pré-construits (variété visuelle, partagés entre toutes les instances)."
        )]
        [SerializeField]
        private int _treeVariantCount = 12;

        [Tooltip("Taille d'un mini-cube d'arbre, en unités monde (plus petit = plus fin).")]
        [SerializeField]
        private float _treeVoxelUnit = 0.32f;

        [Tooltip("Hauteur du tronc, en mini-cubes (min/max).")]
        [SerializeField]
        private int _trunkHeightMin = 10;

        [SerializeField]
        private int _trunkHeightMax = 18;

        [Tooltip("Rayon du houppier, en mini-cubes (min/max).")]
        [SerializeField]
        private int _canopyRadiusMin = 6;

        [SerializeField]
        private int _canopyRadiusMax = 10;

        [Header("Désert")]
        [SerializeField]
        private float _desertPropDensity = 0.018f;

        [SerializeField]
        private int _desertPropMinSpacing = 5;

        [SerializeField]
        private int _desertPropVariantCount = 10;

        [Header("Neige")]
        [SerializeField]
        private float _snowPropDensity = 0.014f;

        [SerializeField]
        private int _snowPropMinSpacing = 6;

        [SerializeField]
        private int _snowPropVariantCount = 10;

        [Header("Marais")]
        [SerializeField]
        private float _swampPropDensity = 0.02f;

        [SerializeField]
        private int _swampPropMinSpacing = 5;

        [SerializeField]
        private int _swampPropVariantCount = 12;

        [Tooltip("Taille d'un mini-cube de prop (désert/neige/marais), en unités monde.")]
        [SerializeField]
        private float _propVoxelUnit = 0.28f;

        public float GrassTuftDensity => _grassTuftDensity;
        public float GrassTuftMinSize => _grassTuftMinSize;
        public float GrassTuftMaxSize => _grassTuftMaxSize;
        public float GrassTuftMinWidth => _grassTuftMinWidth;
        public float GrassTuftMaxWidth => _grassTuftMaxWidth;

        public float FlowerDensity => _flowerDensity;
        public float FlowerMinSize => _flowerMinSize;
        public float FlowerMaxSize => _flowerMaxSize;
        public float FlowerMinWidth => _flowerMinWidth;
        public float FlowerMaxWidth => _flowerMaxWidth;

        public float TreeDensity => _treeDensity;
        public int TreeMinSpacing => _treeMinSpacing;
        public int TreeVariantCount => _treeVariantCount;
        public float TreeVoxelUnit => _treeVoxelUnit;
        public int TrunkHeightMin => _trunkHeightMin;
        public int TrunkHeightMax => _trunkHeightMax;
        public int CanopyRadiusMin => _canopyRadiusMin;
        public int CanopyRadiusMax => _canopyRadiusMax;

        public float DesertPropDensity => _desertPropDensity;
        public int DesertPropMinSpacing => _desertPropMinSpacing;
        public int DesertPropVariantCount => _desertPropVariantCount;

        public float SnowPropDensity => _snowPropDensity;
        public int SnowPropMinSpacing => _snowPropMinSpacing;
        public int SnowPropVariantCount => _snowPropVariantCount;

        public float SwampPropDensity => _swampPropDensity;
        public int SwampPropMinSpacing => _swampPropMinSpacing;
        public int SwampPropVariantCount => _swampPropVariantCount;

        public float PropVoxelUnit => _propVoxelUnit;
    }
}
