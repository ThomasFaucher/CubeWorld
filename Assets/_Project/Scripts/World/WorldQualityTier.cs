using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Traduit le palier <see cref="QualitySettings"/> actif (Low/Medium/High, voir
    /// ProjectSettings/QualitySettings.asset) en distance de vue/LOD du monde voxel.
    ///
    /// Les paliers eux-mêmes pilotent déjà ombres/MSAA/résolution de shadowmap via leur
    /// UniversalRenderPipelineAsset dédié (Assets/Settings/Low_RPAsset.asset,
    /// Medium_RPAsset.asset, PC_RPAsset.asset pour High) ; la distance de vue, elle, est un
    /// champ de gameplay propre à ce projet (WorldConfig), pas un réglage QualitySettings
    /// natif — ce pont fait le lien entre les deux.
    /// </summary>
    internal static class WorldQualityTier
    {
        private readonly struct Preset
        {
            public readonly int ViewDistance;
            public readonly int VerticalViewDistance;
            public readonly int LodNearDistance;

            public Preset(int viewDistance, int verticalViewDistance, int lodNearDistance)
            {
                ViewDistance = viewDistance;
                VerticalViewDistance = verticalViewDistance;
                LodNearDistance = lodNearDistance;
            }
        }

        // Index attendu = celui de ProjectSettings/QualitySettings.asset (0=Low, 1=Medium,
        // 2=High). High reprend les valeurs par défaut historiques de WorldConfig (8/4/3) :
        // le palier par défaut du projet (m_CurrentQuality: 2) ne change donc rien au
        // comportement d'avant l'introduction des paliers.
        private static readonly Preset[] Presets =
        {
            new(viewDistance: 5, verticalViewDistance: 2, lodNearDistance: 1),
            new(viewDistance: 7, verticalViewDistance: 3, lodNearDistance: 2),
            new(viewDistance: 8, verticalViewDistance: 4, lodNearDistance: 3),
        };

        /// <summary>
        /// Applique le palier actif à cette copie runtime de <see cref="WorldConfig"/> (jamais
        /// à l'asset disque — voir WorldBootstrap.Awake). Sans effet si le palier actif est
        /// hors de la liste attendue (projet mal configuré) : garde alors les valeurs de
        /// l'asset telles quelles plutôt que de planter.
        /// </summary>
        public static void Apply(WorldConfig config)
        {
            int level = QualitySettings.GetQualityLevel();
            if (level < 0 || level >= Presets.Length)
            {
                return;
            }

            Preset preset = Presets[level];
            config.SetViewDistance(preset.ViewDistance, preset.VerticalViewDistance);
            config.SetLodNearDistance(preset.LodNearDistance);
        }
    }
}
