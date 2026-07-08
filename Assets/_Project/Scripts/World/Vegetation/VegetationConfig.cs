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

        [Header("Arbres")]
        [Tooltip("Probabilité qu'une colonne Grass en biome Forêt porte un arbre.")]
        [SerializeField]
        private float _treeDensity = 0.02f;

        [Tooltip("Distance minimale (en voxels) entre deux arbres acceptés dans le même chunk.")]
        [SerializeField]
        private int _treeMinSpacing = 4;

        [Tooltip(
            "Nombre de meshes d'arbre pré-construits (variété visuelle, partagés entre toutes les instances)."
        )]
        [SerializeField]
        private int _treeVariantCount = 4;

        [Tooltip("Taille d'un mini-cube d'arbre, en unités monde.")]
        [SerializeField]
        private float _treeVoxelUnit = 0.35f;

        [Tooltip("Hauteur du tronc, en mini-cubes (min/max).")]
        [SerializeField]
        private int _trunkHeightMin = 5;

        [SerializeField]
        private int _trunkHeightMax = 8;

        [Tooltip("Rayon du houppier, en mini-cubes (min/max).")]
        [SerializeField]
        private int _canopyRadiusMin = 4;

        [SerializeField]
        private int _canopyRadiusMax = 6;

        public float GrassTuftDensity => _grassTuftDensity;
        public float GrassTuftMinSize => _grassTuftMinSize;
        public float GrassTuftMaxSize => _grassTuftMaxSize;

        public float TreeDensity => _treeDensity;
        public int TreeMinSpacing => _treeMinSpacing;
        public int TreeVariantCount => _treeVariantCount;
        public float TreeVoxelUnit => _treeVoxelUnit;
        public int TrunkHeightMin => _trunkHeightMin;
        public int TrunkHeightMax => _trunkHeightMax;
        public int CanopyRadiusMin => _canopyRadiusMin;
        public int CanopyRadiusMax => _canopyRadiusMax;
    }
}
