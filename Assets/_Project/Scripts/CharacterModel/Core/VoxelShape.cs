namespace CubeWorld.CharacterModel.Core
{
    /// <summary>Primitives volumiques que <see cref="VoxelStamper"/> sait tamponner dans une grille.</summary>
    internal enum VoxelShape
    {
        Box,

        /// <summary>Pavé aux coins verticaux arrondis (section elliptique en X/Z).</summary>
        Column,

        /// <summary>Ellipsoïde inscrit dans le pavé demandé.</summary>
        Sphere,

        /// <summary>Ellipsoïde tronqué par le bas (calotte).</summary>
        Dome,

        /// <summary>Cône plein à la base, pointe centrale en haut.</summary>
        ConeUp,

        /// <summary>Cône plein en haut, pointe centrale en bas.</summary>
        ConeDown,
    }
}
