using UnityEngine;

namespace CubeWorld.CharacterModel.Core
{
    /// <summary>
    /// Pipeline d'une pièce de personnage : une grille voxel + un pivot (en coordonnées
    /// voxel) + l'échelle monde. Le pivot est le point d'attache/rotation de la pièce
    /// (épaule pour un bras, hanche pour une jambe, base du cou pour la tête...) : il
    /// devient l'origine du mesh généré.
    /// </summary>
    internal sealed class VoxelPartMeshBuilder
    {
        public VoxelGrid Grid { get; }
        public Vector3 PivotVoxels { get; }
        public float Unit { get; }

        public VoxelPartMeshBuilder(
            int sizeX,
            int sizeY,
            int sizeZ,
            Vector3 pivotVoxels,
            float unit
        )
        {
            Grid = new VoxelGrid(sizeX, sizeY, sizeZ);
            PivotVoxels = pivotVoxels;
            Unit = unit;
        }

        /// <summary>
        /// Convertit une position voxel de cette grille en position locale au mesh (unités
        /// monde, pivot à l'origine). Sert à calculer les positions des sockets enfants.
        /// </summary>
        public Vector3 VoxelToLocal(Vector3 voxelPosition) => (voxelPosition - PivotVoxels) * Unit;

        public Mesh ToMesh(string meshName, int colorSeed) =>
            VoxelGridMesher.BuildMesh(Grid, meshName, Unit, PivotVoxels, colorSeed);
    }
}
