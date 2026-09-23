using CubeWorld.CharacterModel.Core;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel.Parts
{
    /// <summary>
    /// Jambe en deux pièces : jambe (colonne courte et épaisse, pivot en HAUT = hanche,
    /// encastrée de 2 rangées dans le pelvis pour que le balancement ne montre jamais de
    /// jour) et pied (bottine arrondie plus large que la jambe, pivot en haut = cheville).
    /// </summary>
    internal static class LegGenerator
    {
        public static BodyPart BuildLeg(
            int thickness,
            int height,
            CharacterPalette palette,
            float unit,
            int colorSeed
        )
        {
            var part = new VoxelPartMeshBuilder(
                thickness,
                height,
                thickness,
                new Vector3(thickness / 2f, height, thickness / 2f),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Colonne de pantalon + 2 rangées de "chaussette" sombre au-dessus de la bottine.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                thickness,
                height,
                thickness,
                VoxelShape.Column,
                palette.Pants
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                thickness,
                2,
                thickness,
                VoxelShape.Column,
                CharacterPalette.Darken(palette.Pants, 0.8f)
            );

            Mesh mesh = part.ToMesh("CharacterLeg", colorSeed);

            return new BodyPart(
                CharacterRigDefinition.LegL, // renommé au moment de l'assemblage (L/R)
                mesh,
                // Cheville 1 rangée au-dessus du bas : le haut de la bottine chevauche la
                // jambe, aucun jour visible quand le pied fléchit.
                new PartSocket(
                    CharacterRigDefinition.SocketAnkle,
                    part.VoxelToLocal(new Vector3(thickness / 2f, 1f, thickness / 2f))
                )
            );
        }

        public static BodyPart BuildFoot(
            int legThickness,
            CharacterPalette palette,
            float unit,
            int colorSeed
        )
        {
            int width = legThickness + 2;
            int depth = legThickness + 3;
            const int height = 3;

            // Pivot : sommet de la tige, aligné sur l'axe de la jambe — la bottine déborde
            // d'1 voxel derrière le talon et de 2 devant les orteils.
            var part = new VoxelPartMeshBuilder(
                width,
                height,
                depth,
                new Vector3(width / 2f, height, 1f + (legThickness / 2f)),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Semelle pleine longueur, corps de bottine arrondi, tige plus courte à l'arrière
            // (le décroché dessine la pointe du pied).
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                width,
                1,
                depth,
                VoxelShape.Box,
                palette.BootSole
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 1, 0),
                width,
                1,
                depth,
                VoxelShape.Column,
                palette.Boot
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 2, 0),
                width,
                1,
                legThickness + 2,
                VoxelShape.Column,
                palette.Boot
            );

            // Petit accent doré sur la pointe.
            grid.PaintFrontmost((width / 2) - 1, 1, palette.Emblem);
            grid.PaintFrontmost(width / 2, 1, palette.Emblem);

            Mesh mesh = part.ToMesh("CharacterFoot", colorSeed);
            return new BodyPart(CharacterRigDefinition.FootL, mesh);
        }
    }
}
