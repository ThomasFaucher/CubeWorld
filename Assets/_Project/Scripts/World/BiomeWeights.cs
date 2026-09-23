namespace CubeWorld.World
{
    /// <summary>
    /// Poids d'appartenance (0..1, somme = 1) de chaque biome à un point du monde — une
    /// version "molle" de <see cref="BiomeShape.Classify"/> (tout-ou-rien) utilisée
    /// uniquement pour moduler le relief (voir <see cref="BiomeHeightProfile"/>) : mélanger
    /// des profils de hauteur par ces poids plutôt que de basculer durement de l'un à
    /// l'autre évite les falaises à la frontière de deux biomes (voir le commentaire
    /// historique dans TerrainGenerationJob sur la dépression Marais abandonnée pour
    /// cette raison). Le matériau de surface et les props restent pilotés par la
    /// classification dure, qui garde un sens précis ("ce bloc-ci est en Désert").
    /// </summary>
    public readonly struct BiomeWeights
    {
        public readonly float Plains;
        public readonly float Forest;
        public readonly float Desert;
        public readonly float Snow;
        public readonly float Swamp;

        public BiomeWeights(float plains, float forest, float desert, float snow, float swamp)
        {
            Plains = plains;
            Forest = forest;
            Desert = desert;
            Snow = snow;
            Swamp = swamp;
        }
    }
}
