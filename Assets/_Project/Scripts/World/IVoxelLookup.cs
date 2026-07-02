using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Accès en lecture aux voxels par coordonnées monde. Permet au builder de
    /// mesh d'interroger les voxels voisins sans connaître la classe monde.
    /// </summary>
    public interface IVoxelLookup
    {
        /// <summary>Voxel à la position monde donnée ; Air si hors des chunks chargés.</summary>
        Voxel GetVoxel(int3 worldPosition);
    }
}
