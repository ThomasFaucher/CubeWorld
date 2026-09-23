using CubeWorld.CharacterModel.Core;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel.Parts
{
    /// <summary>
    /// Rendu voxel de l'équipement. Pour l'instant : une épée portée dans le dos — un élément
    /// de SILHOUETTE à part entière (lame volumineuse, garde dorée nette, pommeau), pas une
    /// planche fine. Pensée pour s'attacher à l'ancre <see cref="Rig.CharacterRigDefinition.GearBackAnchor"/>
    /// du torse et suivre le buste dans l'animation, comme le faisaient les autres pièces.
    ///
    /// NOTE : ce générateur n'est PAS appelé par <see cref="CharacterModelBuilder"/> — l'arme
    /// affichée est un élément à part, piloté par du code cross-assemblée (le PlayerGearVisual
    /// du joueur monte/démonte la pièce sur l'ancre selon le type d'arme réellement équipée)
    /// pas par le personnage lui-même. Public : appelé depuis l'assemblée CubeWorld.Player.
    /// </summary>
    public static class GearGenerator
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

        /// <summary>
        /// Pioche pointe en bas (port dans le dos, second point de port — voir
        /// <see cref="Rig.CharacterRigDefinition.SocketBackTool"/>) : manche bois fin, poignée
        /// de cuir près du pivot (= là où la main l'attrape quand elle est tenue en main), puis
        /// fer en tête — barre acier centrale à deux tons, avec deux pointes tapérées de part
        /// et d'autre (silhouette classique "deux cornes"). Grille 16 x 26 x 4.
        /// </summary>
        public static BodyPart BuildPickaxe(CharacterPalette palette, float unit, int colorSeed)
        {
            const int gridW = 16;
            const int gridH = 26;
            const int gridD = 4;

            // Layout vertical (bas -> haut) : manche 0-15, poignée de cuir 11-14 (recouvre le
            // haut du manche), collier métallique 15, fer 16-21 (+ pointes tapérées de part et
            // d'autre). Pivot au centre de la poignée : c'est la zone tenue en main.
            var part = new VoxelPartMeshBuilder(
                gridW,
                gridH,
                gridD,
                new Vector3(8f, 13f, 2f),
                unit
            );
            VoxelGrid grid = part.Grid;

            const int handleW = 2;
            const int handleD = 2;
            int handleX = (gridW - handleW) / 2;
            int handleZ = (gridD - handleD) / 2;

            // Manche : colonne fine du sol jusqu'au fer.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(handleX, 0, handleZ),
                handleW,
                16,
                handleD,
                VoxelShape.Column,
                palette.LeatherLight
            );

            // Poignée de cuir : recouvre le haut du manche, juste sous le fer — la zone tenue
            // en main quand l'outil est dégainé.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(handleX, 11, handleZ),
                handleW,
                4,
                handleD,
                VoxelShape.Column,
                palette.Belt
            );

            // Collier métallique : sépare visuellement le manche du fer.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(6, 15, 0),
                4,
                1,
                gridD,
                VoxelShape.Box,
                palette.Emblem
            );

            // Fer : barre centrale à deux tons (flanc ombré + méplat clair, comme la lame de
            // l'épée) puis deux pointes tapérées de part et d'autre.
            const int headBottom = 16;
            const int headTop = 21;
            const int headLeft = 3;
            const int headRight = 12;

            VoxelStamper.Stamp(
                grid,
                new Vector3Int(headLeft, headBottom, 0),
                headRight - headLeft + 1,
                headTop - headBottom + 1,
                gridD,
                VoxelShape.Box,
                palette.BladeDark
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(headLeft + 1, headBottom + 1, 1),
                headRight - headLeft - 1,
                headTop - headBottom - 1,
                gridD - 2,
                VoxelShape.Box,
                palette.Blade
            );

            // Pointes : la barre se resserre par paliers vers l'extérieur jusqu'à un point
            // (silhouette "deux cornes") — pas de primitive VoxelStamper pour un cône couché
            // sur le côté (ConeUp/ConeDown ne tapèrent qu'à la verticale), donc écrit voxel par
            // voxel.
            for (int step = 1; ; step++)
            {
                int top = headTop - (step - 1);
                int bottom = headBottom + (step - 1);
                if (bottom > top)
                {
                    break;
                }

                int leftX = headLeft - step;
                int rightX = headRight + step;
                for (int y = bottom; y <= top; y++)
                {
                    grid.Set(leftX, y, 1, palette.BladeDark);
                    grid.Set(leftX, y, 2, palette.BladeDark);
                    grid.Set(rightX, y, 1, palette.BladeDark);
                    grid.Set(rightX, y, 2, palette.BladeDark);
                }
            }

            Mesh mesh = part.ToMesh("CharacterPickaxeBack", colorSeed);
            return new BodyPart(CharacterRigDefinition.PickaxeBack, mesh);
        }
    }
}
