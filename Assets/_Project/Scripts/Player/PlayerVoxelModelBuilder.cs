using CubeWorld.Player.VoxelModels;
using CubeWorld.Player.VoxelModels.Generation;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Point d'entrée : délègue la construction du mesh au builder de l'archétype
    /// choisi (un fichier .cs par personnage dans VoxelModels/).
    ///
    /// Le Swordsman est procédural (piloté par <paramref name="seed"/> et
    /// <paramref name="expression"/>) ; les autres archétypes restent pour l'instant
    /// des silhouettes figées et ignorent ces deux paramètres.
    /// </summary>
    public static class PlayerVoxelModelBuilder
    {
        public static Mesh Build(
            float targetHeight,
            PlayerArchetype archetype = PlayerArchetype.Swordsman,
            int seed = 0,
            CharacterExpression expression = CharacterExpression.Neutral
        ) =>
            archetype switch
            {
                PlayerArchetype.Archer => ArcherVoxelModel.Build(targetHeight),
                PlayerArchetype.Mage => MageVoxelModel.Build(targetHeight),
                PlayerArchetype.Elf => ElfVoxelModel.Build(targetHeight),
                _ => SwordsmanVoxelModel.Build(targetHeight, seed, expression),
            };
    }
}
