using CubeWorld.Player.CharacterModel.Core;
using CubeWorld.Player.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Parts
{
    /// <summary>
    /// Bassin : socle du personnage (pantalon + ceinture à boucle), pivot au bas de la pièce.
    /// Expose les sockets Torso (dessus), HipL/HipR (dessous, encastrés de 2 rangées dans le
    /// pelvis pour que le haut des jambes reste caché quand elles se balancent). Un "plug"
    /// central dépasse vers le haut, glissé À L'INTÉRIEUR de la taille du torse : il comble le
    /// jour qui s'ouvrirait quand le torse s'incline, tout en restant invisible.
    /// </summary>
    internal static class HipsGenerator
    {
        /// <summary>Rangées d'encastrement des jambes dans le pelvis.</summary>
        public const int LegEmbed = 2;

        public static BodyPart Build(
            int torsoWidth,
            int torsoDepth,
            int hipsHeight,
            int legThickness,
            CharacterPalette palette,
            float unit,
            int colorSeed
        )
        {
            // Pelvis aussi large que le buste : silhouette trapue, et assez de place pour
            // encastrer deux jambes épaisses avec 1 voxel de marge côté extérieur.
            int width = torsoWidth;
            int depth = torsoDepth - 1;

            int plugWidth = torsoWidth - 6;
            int plugDepth = torsoDepth - 4;

            var part = new VoxelPartMeshBuilder(
                width,
                hipsHeight + 2,
                depth,
                new Vector3(width / 2f, 0f, depth / 2f),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Pelvis = bas de la tunique (jupe verte) + ceinture de cuir sur la rangée du
            // haut, boucle dorée devant : la tunique du héros descend sur les hanches.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                width,
                hipsHeight - 1,
                depth,
                VoxelShape.Column,
                palette.OutfitPrimary
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, hipsHeight - 1, 0),
                width,
                1,
                depth,
                VoxelShape.Column,
                palette.Belt
            );

            // Boucle de ceinture : 2 voxels recolorés sur la surface avant.
            grid.PaintFrontmost((width / 2) - 1, hipsHeight - 1, palette.Emblem);
            grid.PaintFrontmost(width / 2, hipsHeight - 1, palette.Emblem);

            // Plug caché dans la taille du torse (section plus petite d'au moins 1 voxel de
            // chaque côté que la taille : jamais visible, jamais de z-fighting).
            VoxelStamper.Stamp(
                grid,
                new Vector3Int((width - plugWidth) / 2, hipsHeight, (depth - plugDepth) / 2),
                plugWidth,
                2,
                plugDepth,
                VoxelShape.Column,
                palette.OutfitSecondary
            );

            // Jambes écartées au maximum SANS jamais affleurer le bord du pelvis (marge d'au
            // moins 1 voxel : évite les faces coplanaires) ni se chevaucher entre elles.
            float legSeparation = Mathf.Max(
                legThickness / 2f,
                (width / 2f) - (legThickness / 2f) - 1f
            );

            Mesh mesh = part.ToMesh("CharacterHips", colorSeed);

            return new BodyPart(
                CharacterRigDefinition.Hips,
                mesh,
                new PartSocket(
                    CharacterRigDefinition.SocketTorso,
                    part.VoxelToLocal(new Vector3(width / 2f, hipsHeight, depth / 2f))
                ),
                new PartSocket(
                    CharacterRigDefinition.SocketHipL,
                    part.VoxelToLocal(
                        new Vector3((width / 2f) - legSeparation, LegEmbed, depth / 2f)
                    )
                ),
                new PartSocket(
                    CharacterRigDefinition.SocketHipR,
                    part.VoxelToLocal(
                        new Vector3((width / 2f) + legSeparation, LegEmbed, depth / 2f)
                    )
                )
            );
        }
    }
}
