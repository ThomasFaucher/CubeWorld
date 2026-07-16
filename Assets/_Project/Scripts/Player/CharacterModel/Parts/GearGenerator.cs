using CubeWorld.Player.CharacterModel.Core;
using CubeWorld.Player.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Parts
{
    /// <summary>
    /// Équipement décoratif du personnage. Pour l'instant : l'épée portée dans le dos —
    /// un élément de SILHOUETTE à part entière (lame volumineuse, garde dorée nette,
    /// pommeau), pas une planche fine. Attachée au socket Back du torse, elle suit le
    /// buste dans l'animation.
    ///
    /// NOTE : cette épée est purement visuelle. Quand l'équipement réel (PlayerEquipment)
    /// pilotera l'arme affichée, ce générateur deviendra le rendu voxel des armes d'après
    /// leur ItemDefinition, et la pièce sera montée/démontée dynamiquement.
    /// </summary>
    internal static class GearGenerator
    {
        /// <summary>
        /// Épée pointe en bas (port dans le dos) : pommeau et poignée en haut, garde au
        /// milieu (pivot), lame large à deux tons vers le bas. Grille 8 x 27 x 3.
        /// </summary>
        public static BodyPart BuildSword(CharacterPalette palette, float unit, int colorSeed)
        {
            const int gridW = 8;
            const int gridH = 27;
            const int gridD = 3;

            // Layout vertical (bas -> haut) : pointe 0-2, lame 3-18, garde 19-20,
            // poignée 21-24, pommeau 25-26. Pivot au centre de la garde.
            var part = new VoxelPartMeshBuilder(
                gridW,
                gridH,
                gridD,
                new Vector3(4f, 20f, 1.5f),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Lame : 4 de large, 2 d'épaisseur — flancs ombrés puis arête centrale claire
            // par-dessus (la bande claire lit comme le méplat ciselé de la lame).
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 3, 0),
                4,
                16,
                2,
                VoxelShape.Box,
                palette.BladeDark
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(3, 3, 0),
                2,
                16,
                2,
                VoxelShape.Box,
                palette.Blade
            );

            // Pointe : cône inversé qui referme la lame vers le bas.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 0, 0),
                4,
                3,
                2,
                VoxelShape.ConeDown,
                palette.Blade
            );

            // Garde dorée pleine largeur : le seul élément vraiment saturé de l'épée,
            // volume net qui sépare lame et poignée.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 19, 0),
                8,
                2,
                3,
                VoxelShape.Box,
                palette.Emblem
            );

            // Poignée de cuir sombre + pommeau doré.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(3, 21, 0),
                2,
                4,
                2,
                VoxelShape.Box,
                palette.Belt
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 25, 0),
                4,
                2,
                2,
                VoxelShape.Box,
                palette.Emblem
            );

            Mesh mesh = part.ToMesh("CharacterSwordBack", colorSeed);
            return new BodyPart(CharacterRigDefinition.SwordBack, mesh);
        }
    }
}
