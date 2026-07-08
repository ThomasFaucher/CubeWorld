using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Configuration des nuages, éditable dans l'inspecteur — même pattern que
    /// <see cref="WorldConfig"/>/<see cref="VegetationConfig"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "CloudConfig", menuName = "CubeWorld/Cloud Config")]
    public sealed class CloudConfig : ScriptableObject
    {
        [Header("Pool")]
        [Tooltip("Nombre de nuages en pool fixe.")]
        [SerializeField]
        private int _cloudCount = 40;

        [Tooltip(
            "Nombre de meshes de nuage pré-construits (variété visuelle, partagés entre les instances)."
        )]
        [SerializeField]
        private int _cloudVariantCount = 4;

        [Tooltip(
            "Taille d'un mini-cube de nuage, en unités monde (plus gros que les mini-cubes d'arbre pour rester lisible de loin)."
        )]
        [SerializeField]
        private float _cloudUnit = 2f;

        [Header("Vent")]
        [Tooltip("Direction du vent, en XZ (normalisée à l'usage).")]
        [SerializeField]
        private Vector2 _windDirection = new(1f, 0.3f);

        [Tooltip("Vitesse du vent, en unités monde par seconde.")]
        [SerializeField]
        private float _windSpeed = 3f;

        [Header("Position")]
        [Tooltip(
            "Altitude minimale, en unités monde (nettement au-dessus de MaxTerrainHeight * VoxelSize)."
        )]
        [SerializeField]
        private float _altitudeMin = 80f;

        [Tooltip("Altitude maximale, en unités monde.")]
        [SerializeField]
        private float _altitudeMax = 120f;

        [Tooltip(
            "Distance XZ à la cible au-delà de laquelle un nuage est recyclé — c'est aussi la distance à laquelle les nuages apparaissent à l'horizon."
        )]
        [SerializeField]
        private float _recycleRadius = 450f;

        public int CloudCount => _cloudCount;
        public int CloudVariantCount => _cloudVariantCount;
        public float CloudUnit => _cloudUnit;
        public Vector2 WindDirection => _windDirection;
        public float WindSpeed => _windSpeed;
        public float AltitudeMin => _altitudeMin;
        public float AltitudeMax => _altitudeMax;
        public float RecycleRadius => _recycleRadius;
    }
}
