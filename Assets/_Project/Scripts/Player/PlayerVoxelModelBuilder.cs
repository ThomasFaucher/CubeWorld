using CubeWorld.Player.VoxelModels;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Point d'entrée : délègue la construction du mesh au builder de l'archétype
    /// choisi (un fichier .cs par personnage dans VoxelModels/).
    /// </summary>
    public static class PlayerVoxelModelBuilder
    {
        public static Mesh Build(float targetHeight, PlayerArchetype archetype = PlayerArchetype.Swordsman) =>
            archetype switch
            {
                PlayerArchetype.Archer => ArcherVoxelModel.Build(targetHeight),
                PlayerArchetype.Mage => MageVoxelModel.Build(targetHeight),
                PlayerArchetype.Elf => ElfVoxelModel.Build(targetHeight),
                _ => SwordsmanVoxelModel.Build(targetHeight),
            };
    }
}
