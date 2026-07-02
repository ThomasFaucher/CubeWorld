using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Accès en lecture aux voxels par coordonnées monde, sans dépendre de la
    /// classe monde concrète. Utilisé par tout code hors du pipeline de
    /// streaming (futur raycast, édition de blocs...) — le meshing, lui, lit
    /// directement les NativeArray des chunks voisins via des jobs Burst.
    /// </summary>
    public interface IVoxelLookup
    {
        /// <summary>Voxel à la position monde donnée ; Air si hors des chunks chargés.</summary>
        Voxel GetVoxel(int3 worldPosition);
    }
}
