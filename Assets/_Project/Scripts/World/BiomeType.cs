namespace CubeWorld.World
{
    /// <summary>
    /// Biome climatique d'une colonne (x, z) du monde. Détermine le matériau
    /// de surface et l'éligibilité à la végétation (voir BiomeShape, TerrainShape).
    /// </summary>
    public enum BiomeType : byte
    {
        Plains = 0,
        Forest = 1,
        Desert = 2,
        Snow = 3,
    }
}
