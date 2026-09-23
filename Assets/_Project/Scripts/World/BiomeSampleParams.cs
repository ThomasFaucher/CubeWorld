using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Paramètres de bruit nécessaires pour échantillonner un biome via
    /// <see cref="BiomeShape.Sample"/> — partagés entre génération terrain et
    /// meshing (teintes vertex), pour rester parfaitement synchrones.
    /// </summary>
    public readonly struct BiomeSampleParams
    {
        public readonly float2 TemperatureSeedOffset;
        public readonly float2 HumiditySeedOffset;
        public readonly float NoiseScale;
        public readonly int Octaves;
        public readonly float SnowTemperatureThreshold;
        public readonly float DesertTemperatureThreshold;
        public readonly float DesertHumidityThreshold;
        public readonly float SwampHumidityThreshold;
        public readonly float ForestHumidityThreshold;

        public BiomeSampleParams(
            float2 temperatureSeedOffset,
            float2 humiditySeedOffset,
            float noiseScale,
            int octaves,
            float snowTemperatureThreshold,
            float desertTemperatureThreshold,
            float desertHumidityThreshold,
            float swampHumidityThreshold,
            float forestHumidityThreshold)
        {
            TemperatureSeedOffset = temperatureSeedOffset;
            HumiditySeedOffset = humiditySeedOffset;
            NoiseScale = noiseScale;
            Octaves = octaves;
            SnowTemperatureThreshold = snowTemperatureThreshold;
            DesertTemperatureThreshold = desertTemperatureThreshold;
            DesertHumidityThreshold = desertHumidityThreshold;
            SwampHumidityThreshold = swampHumidityThreshold;
            ForestHumidityThreshold = forestHumidityThreshold;
        }

        public BiomeType Sample(int worldX, int worldZ)
        {
            return BiomeShape.Sample(
                worldX,
                worldZ,
                TemperatureSeedOffset,
                HumiditySeedOffset,
                NoiseScale,
                Octaves,
                SnowTemperatureThreshold,
                DesertTemperatureThreshold,
                DesertHumidityThreshold,
                SwampHumidityThreshold,
                ForestHumidityThreshold);
        }

        /// <summary>Poids lissés par biome (voir <see cref="BiomeShape.SampleWeights"/>) — pour le relief.</summary>
        public BiomeWeights SampleWeights(int worldX, int worldZ)
        {
            return BiomeShape.SampleWeights(
                worldX,
                worldZ,
                TemperatureSeedOffset,
                HumiditySeedOffset,
                NoiseScale,
                Octaves);
        }
    }
}
