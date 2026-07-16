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

    }
}
