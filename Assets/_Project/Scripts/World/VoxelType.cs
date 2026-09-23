namespace CubeWorld.World
{
    /// <summary>
    /// Types de voxels du monde. Stocké sur un octet pour garder les chunks
    /// compacts en mémoire (un chunk 32³ = 32 768 octets).
    /// Style CubeWorld : chaque type porte une couleur (pas de textures),
    /// la palette est définie côté génération de mesh.
    /// </summary>
    public enum VoxelType : byte
    {
        Air = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Sand = 4,
        Snow = 5,
        Water = 6,
        Ice = 7,

        // Minerais : ne remplacent que de la Pierre profonde (voir CaveShape.ApplyOreVein),
        // rareté croissante avec la profondeur.
        OreCopper = 8,
        OreIron = 9,
        OreGold = 10,
    }
}
