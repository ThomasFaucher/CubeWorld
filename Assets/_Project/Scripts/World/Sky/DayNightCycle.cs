using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Fait tourner une <see cref="Light"/> directionnelle sur un cycle jour/nuit configurable
    /// (voir <see cref="DayNightConfig"/>) et interpole en continu la couleur/intensité du
    /// soleil ainsi que <see cref="RenderSettings.ambientLight"/>/<see cref="RenderSettings.fogColor"/>
    /// selon la fraction de journée — une seule fonction continue (cosinus de l'élévation du
    /// soleil, lissée), donc jamais de saut brusque entre les phases.
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        // Fraction de journée : 0 = minuit, 0.25 = aube, 0.5 = midi, 0.75 = crépuscule.
        private const float DawnFraction = 0.25f;

        // Largeur (en "élévation" cosinus, -1..1) de la bande aube/crépuscule autour de
        // l'horizon : au-delà, jour ou nuit plein.
        private const float HorizonBand = 0.15f;

        [Tooltip("Lumière directionnelle pilotée par ce cycle (le « soleil » de la scène).")]
        [SerializeField]
        private Light _sunLight;

        [Tooltip("Configuration du cycle. Si vide, des valeurs par défaut sont utilisées.")]
        [SerializeField]
        private DayNightConfig _config;

        private DayNightConfig config;
        private float azimuthDegrees;
        private float dayFraction;

        /// <summary>Fraction de journée actuelle (0 = minuit, 0.5 = midi).</summary>
        public float DayFraction => dayFraction;

        private void Awake()
        {
            config = _config != null ? _config : ScriptableObject.CreateInstance<DayNightConfig>();
            dayFraction = config.StartDayFraction;

            if (_sunLight != null)
            {
                // Conserve l'azimut (rotation Y) déjà réglé dans la scène : seule l'élévation
                // (rotation X) est pilotée par le cycle.
                azimuthDegrees = _sunLight.transform.localEulerAngles.y;
            }

            Apply();
        }

        private void Update()
        {
            dayFraction += Time.deltaTime / Mathf.Max(1f, config.DayLengthSeconds);
            dayFraction %= 1f;
            Apply();
        }

        private void Apply()
        {
            // Élévation du soleil : -1 à minuit, +1 à midi, 0 exactement à l'horizon
            // (aube/crépuscule) — une seule fonction lisse, pas de branchement par phase.
            float elevation = Mathf.Cos((dayFraction - 0.5f) * 2f * Mathf.PI);
            float dayAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-HorizonBand, HorizonBand, elevation));

            // Pic centré exactement sur la transition (dayAmount ≈ 0.5), nul en plein
            // jour/nuit : la teinte chaude d'aube/crépuscule ne doit jamais teinter le reste.
            float duskDawnAmount = 1f - Mathf.Abs((dayAmount * 2f) - 1f);

            Color sunColor = Color.Lerp(config.NightSunColor, config.DaySunColor, dayAmount);
            sunColor = Color.Lerp(sunColor, config.DuskDawnSunColor, duskDawnAmount);
            float sunIntensity = Mathf.Lerp(config.NightSunIntensity, config.DaySunIntensity, dayAmount);

            if (_sunLight != null)
            {
                float pitch = (dayFraction - DawnFraction) * 360f;
                _sunLight.transform.localRotation = Quaternion.Euler(pitch, azimuthDegrees, 0f);
                _sunLight.color = sunColor;
                _sunLight.intensity = sunIntensity;
            }

            // Réappliqué chaque frame (pas seulement à l'Awake) : WorldBootstrap.Start()
            // désactive le brouillard par défaut, et l'ordre entre Start() d'objets
            // différents n'est pas garanti — s'assurer qu'il reste actif après lui.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(config.NightAmbientColor, config.DayAmbientColor, dayAmount);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.Lerp(config.NightFogColor, config.DayFogColor, dayAmount);
            RenderSettings.fogDensity = Mathf.Lerp(config.NightFogDensity, config.DayFogDensity, dayAmount);
        }
    }
}
