using System;
using Unity.Collections;
using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Un chunk cubique du monde : stocke ses voxels dans un tableau natif plat
    /// (index = x + taille * (y + taille * z)), contigu en mémoire et exploitable
    /// tel quel par les jobs Burst (génération, meshing). Doit être libéré
    /// explicitement (Dispose) quand le chunk est déchargé.
    /// </summary>
    public sealed class Chunk : IDisposable
    {
        /// <summary>Position du chunk dans la grille, en unités de chunks.</summary>
        public int3 Coord { get; }

        /// <summary>Taille d'une arête du chunk, en voxels.</summary>
        public int Size { get; }

        private NativeArray<Voxel> voxels;

        public Chunk(int3 coord, int size)
        {
            Coord = coord;
            Size = size;

            // Pas besoin de mise à zéro : le job de génération remplit tous les voxels.
            voxels = new NativeArray<Voxel>(size * size * size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>Position du coin (0,0,0) du chunk, en coordonnées monde (voxels).</summary>
        public int3 WorldOrigin => Coord * Size;

        /// <summary>Vue native brute des voxels — utilisée par les jobs Burst de génération et de meshing.</summary>
        public NativeArray<Voxel> Voxels => voxels;

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

        public void Dispose()
        {
            if (voxels.IsCreated)
            {
                voxels.Dispose();
            }
        }
    }
}
