using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Un chunk cubique du monde : stocke ses voxels dans un tableau plat
    /// (index = x + taille * (y + taille * z)) pour rester contigu en mémoire.
    /// Classe métier pure, aucune dépendance à la scène Unity.
    /// </summary>
    public sealed class Chunk
    {
        /// <summary>Position du chunk dans la grille, en unités de chunks.</summary>
        public int3 Coord { get; }

        /// <summary>Taille d'une arête du chunk, en voxels.</summary>
        public int Size { get; }

        private readonly Voxel[] voxels;

        public Chunk(int3 coord, int size)
        {
            Coord = coord;
            Size = size;
            voxels = new Voxel[size * size * size];
        }

        /// <summary>Position du coin (0,0,0) du chunk, en coordonnées monde (voxels).</summary>
        public int3 WorldOrigin => Coord * Size;

        /// <summary>Vrai si la coordonnée locale est à l'intérieur du chunk.</summary>
        public bool Contains(int x, int y, int z)
        {
            return x >= 0 && x < Size
                && y >= 0 && y < Size
                && z >= 0 && z < Size;
        }

        public Voxel GetVoxel(int x, int y, int z)
        {
            return voxels[ToIndex(x, y, z)];
        }

        public void SetVoxel(int x, int y, int z, Voxel voxel)
        {
            voxels[ToIndex(x, y, z)] = voxel;
        }

        private int ToIndex(int x, int y, int z)
        {
            return x + Size * (y + Size * z);
        }
    }
}
