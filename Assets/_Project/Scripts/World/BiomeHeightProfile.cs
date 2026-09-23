using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Reshape du relief par biome : chaque biome déforme différemment la MÊME fraction
    /// de hauteur de base (<see cref="TerrainShape.Fbm"/>, dans [0,1]) via une fonction pure
    /// dédiée — dunes en Désert, pics en Neige, ondulations fines en Forêt, aplatissement en
    /// Marais, quasi inchangé en Plaines (référence). <see cref="TerrainShape.SampleHeight"/>
    /// mélange ensuite ces fractions par les <see cref="BiomeWeights"/> lissés de
    /// <see cref="BiomeShape.SampleWeights"/> : comme toutes les fractions partagent le même
    /// bruit de base, le mélange reste cohérent visuellement et jamais abrupt à la frontière
    /// de deux biomes (contrairement à une bascule dure entre profils).
    /// </summary>
    internal static class BiomeHeightProfile
    {
        // Marge au-dessus du niveau de la mer visée par l'aplatissement du Marais : un
        // marais est bas et stagnant, jamais complètement noyé.
        private const float SwampSeaLevelMargin = 0.02f;
        private const float SwampFlattenStrength = 0.85f;

        private const float ForestDetailAmplitude = 0.10f;
        private const float DesertBaseWeight = 0.55f;
        private const float DesertDuneWeight = 0.45f;
        private const float SnowPeakAmplify = 1.7f;
        private const float SnowJaggedAmplitude = 0.18f;

        /// <summary>Fraction de hauteur (0..1) mélangée pour ce point, à multiplier par MaxTerrainHeight.</summary>
        public static float Evaluate(BiomeWeights weights, float2 point, float baseFraction, float seaLevelFraction)
        {
            float fraction = weights.Plains * baseFraction;
            fraction += weights.Forest * Forest(point, baseFraction);
            fraction += weights.Desert * Desert(point, baseFraction);
            fraction += weights.Snow * Snow(point, baseFraction);
            fraction += weights.Swamp * Swamp(baseFraction, seaLevelFraction);
            return math.saturate(fraction);
        }

        // Ondulations fines en plus du relief de base : texture "vallonnée" sans changer
        // l'amplitude générale — un bruit à fréquence plus élevée, indépendant (décalé) du
        // bruit de base pour ne pas simplement l'amplifier bêtement.
        private static float Forest(float2 point, float baseFraction)
        {
            float fine = TerrainShape.Fbm((point * 3.7f) + new float2(91.3f, 12.7f), 3);
            return math.saturate(baseFraction + ((fine - 0.5f) * ForestDetailAmplitude));
        }

        // Dunes : mélange du relief de base avec un bruit "ridge" (1 - |Perlin|, une nappe
        // de crêtes) à fréquence moyenne — des vagues de sable plutôt qu'un relief générique.
        private static float Desert(float2 point, float baseFraction)
        {
            float ridge = 1f - math.abs(noise.cnoise((point * 2.2f) + new float2(-44.1f, 8.9f)));
            return math.saturate((baseFraction * DesertBaseWeight) + (ridge * DesertDuneWeight));
        }

        // Pics : amplifie l'écart à la moyenne du relief de base (les creux restent proches
        // de la moyenne, les crêtes s'envolent) puis ajoute un bruit plus fin pour des
        // arêtes moins lisses qu'une simple colline étirée.
        private static float Snow(float2 point, float baseFraction)
        {
            float amplified = 0.5f + ((baseFraction - 0.5f) * SnowPeakAmplify);
            float jagged = TerrainShape.Fbm((point * 4.5f) + new float2(233.1f, -77.4f), 2);
            return math.saturate(amplified + ((jagged - 0.5f) * SnowJaggedAmplitude));
        }

        // Aplatissement vers un peu au-dessus du niveau de la mer : un marais est bas et
        // stagnant, jamais montagneux — on tire fort la fraction de base vers cette cible.
        private static float Swamp(float baseFraction, float seaLevelFraction)
        {
            float target = seaLevelFraction + SwampSeaLevelMargin;
            return math.lerp(baseFraction, target, SwampFlattenStrength);
        }
    }
}
