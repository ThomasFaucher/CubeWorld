using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Fonctions pures de classification de biome (comme <see cref="TerrainShape"/> pour
    /// le relief) : deux bruits fractals indépendants (température, humidité), à basse
    /// fréquence pour donner de larges régions organiques plutôt qu'un damier. Ne dépend
    /// d'aucun objet managé : appelable depuis le job Burst de génération comme depuis
    /// <see cref="TerrainGenerator.SampleBiome"/> (thread principal, pour la végétation).
    /// </summary>
    internal static class BiomeShape
    {
        // Ancres au centre de chaque région de Classify, en espace (température, humidité)
        // normalisé [0,1]² — voir ClassifyWeights.
        private static readonly float2 PlainsAnchor = new(0.5f, 0.35f);
        private static readonly float2 ForestAnchor = new(0.5f, 0.8f);
        private static readonly float2 DesertAnchor = new(0.85f, 0.15f);
        private static readonly float2 SnowAnchor = new(0.12f, 0.5f);
        private static readonly float2 SwampAnchor = new(0.45f, 0.9f);

        // Plus grand = transitions plus nettes entre profils de relief (mais jamais des
        // vraies falaises : la fonction reste continue quel que soit ce réglage).
        private const float WeightSharpness = 14f;

        /// <summary>Biome de la colonne (x, z) monde.</summary>
        public static BiomeType Sample(
            int worldX,
            int worldZ,
            float2 temperatureSeedOffset,
            float2 humiditySeedOffset,
            float noiseScale,
            int octaves,
            float snowTemperatureThreshold,
            float desertTemperatureThreshold,
            float desertHumidityThreshold,
            float swampHumidityThreshold,
            float forestHumidityThreshold
        )
        {
            SampleClimate(
                worldX,
                worldZ,
                temperatureSeedOffset,
                humiditySeedOffset,
                noiseScale,
                octaves,
                out float temperature,
                out float humidity);

            return Classify(
                temperature,
                humidity,
                snowTemperatureThreshold,
                desertTemperatureThreshold,
                desertHumidityThreshold,
                swampHumidityThreshold,
                forestHumidityThreshold);
        }

        public static void SampleClimate(
            int worldX,
            int worldZ,
            float2 temperatureSeedOffset,
            float2 humiditySeedOffset,
            float noiseScale,
            int octaves,
            out float temperature,
            out float humidity
        )
        {
            var point = new float2(worldX, worldZ);
            temperature = TerrainShape.Fbm((point + temperatureSeedOffset) * noiseScale, octaves);
            humidity = TerrainShape.Fbm((point + humiditySeedOffset) * noiseScale, octaves);
        }

        public static BiomeType Classify(
            float temperature,
            float humidity,
            float snowTemperatureThreshold,
            float desertTemperatureThreshold,
            float desertHumidityThreshold,
            float swampHumidityThreshold,
            float forestHumidityThreshold
        )
        {
            if (temperature < snowTemperatureThreshold)
            {
                return BiomeType.Snow;
            }

            if (temperature > desertTemperatureThreshold && humidity < desertHumidityThreshold)
            {
                return BiomeType.Desert;
            }

            if (
                humidity > swampHumidityThreshold
                && temperature <= desertTemperatureThreshold
            )
            {
                return BiomeType.Swamp;
            }

            return humidity > forestHumidityThreshold ? BiomeType.Forest : BiomeType.Plains;
        }

        /// <summary>Poids lissés (voir <see cref="BiomeWeights"/>) de la colonne (x, z) monde.</summary>
        public static BiomeWeights SampleWeights(
            int worldX,
            int worldZ,
            float2 temperatureSeedOffset,
            float2 humiditySeedOffset,
            float noiseScale,
            int octaves
        )
        {
            SampleClimate(
                worldX,
                worldZ,
                temperatureSeedOffset,
                humiditySeedOffset,
                noiseScale,
                octaves,
                out float temperature,
                out float humidity);

            return ClassifyWeights(temperature, humidity);
        }

        /// <summary>
        /// Poids lissés par noyau RBF (softmax des distances aux ancres ci-dessus) :
        /// toujours positifs, toujours de somme 1, continus partout — pas de seuil dur,
        /// donc pas de transition abrupte à moduler ensuite dans BiomeHeightProfile.
        /// </summary>
        public static BiomeWeights ClassifyWeights(float temperature, float humidity)
        {
            var point = new float2(temperature, humidity);

            float plains = Score(point, PlainsAnchor);
            float forest = Score(point, ForestAnchor);
            float desert = Score(point, DesertAnchor);
            float snow = Score(point, SnowAnchor);
            float swamp = Score(point, SwampAnchor);

            float total = plains + forest + desert + snow + swamp;
            return new BiomeWeights(plains / total, forest / total, desert / total, snow / total, swamp / total);
        }

        private static float Score(float2 point, float2 anchor)
        {
            float distanceSq = math.lengthsq(point - anchor);
            return math.exp(-distanceSq * WeightSharpness);
        }
    }
}
