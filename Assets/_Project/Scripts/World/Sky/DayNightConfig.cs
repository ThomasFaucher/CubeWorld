using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Configuration du cycle jour/nuit, éditable dans l'inspecteur — même pattern que
    /// <see cref="CloudConfig"/>. Les phases (aube/midi/crépuscule/nuit) elles-mêmes sont
    /// une simple fonction continue de la fraction de journée (voir <see cref="DayNightCycle"/>) :
    /// cette config n'expose que les couleurs/intensités aux extrêmes, pas les seuils de
    /// transition, pour rester simple à régler sans pouvoir créer de saut brusque.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "CubeWorld/Day Night Config")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [Header("Durée")]
        [Tooltip("Durée d'un cycle jour/nuit complet, en secondes.")]
        [SerializeField]
        private float _dayLengthSeconds = 300f;

        [Tooltip("Fraction de journée au démarrage (0 = minuit, 0.25 = aube, 0.5 = midi, 0.75 = crépuscule).")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _startDayFraction = 0.28f;

        [Header("Lumière directionnelle")]
        [SerializeField]
        private Color _daySunColor = new(1f, 0.97f, 0.9f);

        [SerializeField]
        private Color _nightSunColor = new(0.35f, 0.42f, 0.62f);

        [Tooltip("Teinte chaude appliquée au pic de la transition aube/crépuscule, par-dessus l'interpolation jour/nuit.")]
        [SerializeField]
        private Color _duskDawnSunColor = new(1f, 0.5f, 0.22f);

        [SerializeField]
        private float _daySunIntensity = 2f;

        [SerializeField]
        private float _nightSunIntensity = 0.05f;

        [Header("Ambiance / brouillard")]
        [SerializeField]
        private Color _dayAmbientColor = new(0.72f, 0.83f, 0.95f);

        [SerializeField]
        private Color _nightAmbientColor = new(0.05f, 0.07f, 0.14f);

        [SerializeField]
        private Color _dayFogColor = new(0.62f, 0.78f, 0.92f);

        [SerializeField]
        private Color _nightFogColor = new(0.02f, 0.03f, 0.08f);

        [Tooltip("Densité du brouillard exponentiel de jour (léger, surtout pour fondre l'horizon).")]
        [SerializeField]
        private float _dayFogDensity = 0.0015f;

        [Tooltip("Densité du brouillard exponentiel de nuit (un peu plus dense, accentue l'obscurité au loin).")]
        [SerializeField]
        private float _nightFogDensity = 0.01f;

        public float DayLengthSeconds => _dayLengthSeconds;
        public float StartDayFraction => _startDayFraction;
        public Color DaySunColor => _daySunColor;
        public Color NightSunColor => _nightSunColor;
        public Color DuskDawnSunColor => _duskDawnSunColor;
        public float DaySunIntensity => _daySunIntensity;
        public float NightSunIntensity => _nightSunIntensity;
        public Color DayAmbientColor => _dayAmbientColor;
        public Color NightAmbientColor => _nightAmbientColor;
        public Color DayFogColor => _dayFogColor;
        public Color NightFogColor => _nightFogColor;
        public float DayFogDensity => _dayFogDensity;
        public float NightFogDensity => _nightFogDensity;
    }
}
