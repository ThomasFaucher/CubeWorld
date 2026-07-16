namespace CubeWorld.World
{
    /// <summary>
    /// Biome climatique d'une colonne (x, z) du monde. Détermine le matériau
    /// de surface, les teintes et l'éligibilité aux props (voir BiomeShape,
    /// TerrainShape, VegetationSpawner).
    /// </summary>
    public enum BiomeType : byte
    {
        Plains = 0,
        Forest = 1,
        Desert = 2,
        Snow = 3,
        Swamp = 4,
    }
}
