using System.Runtime.InteropServices;

namespace CubeWorld.World
{
    /// <summary>
    /// Un voxel du monde. Struct blittable d'un seul octet : compatible
    /// Burst/Jobs et stockable dans des NativeArray sans copie.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Voxel
    {
        public readonly VoxelType Type;

        public Voxel(VoxelType type)
        {
            Type = type;
        }

        /// <summary>Vrai si le voxel bloque la vue et le déplacement.</summary>
        public bool IsSolid => Type != VoxelType.Air && Type != VoxelType.Water;

        /// <summary>Vrai si le voxel est de l'air (aucune face à générer).</summary>
        public bool IsAir => Type == VoxelType.Air;

        public static readonly Voxel Air = new(VoxelType.Air);
    }
}
