using CubeWorld.CharacterModel.Core;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel.Parts
{
    /// <summary>
    /// Bras en deux pièces : bras supérieur (capsule épaisse coiffée d'une ÉPAULIÈRE de cuir
    /// sombre surdimensionnée — plus large que le bras ET que la ligne d'épaule, c'est elle
    /// qui casse la silhouette comme dans la référence CubeWorld ; pivot AU CENTRE de la
    /// boule d'épaule) et avant-bras (colonne + moufle sphérique, pivot au centre du coude).
    /// Les pivots placés au centre des formes rondes rendent les articulations
    /// "rotation-proof" : quel que soit l'angle, la boule couvre le joint, aucun jour
    /// n'apparaît — et l'épaulière tourne avec le bras en couvrant toujours l'épaule.
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : Width est fixe (les chevauchements anti
    /// z-fighting supposent des largeurs paires) ; pour varier l'épaisseur des bras par
    /// seed, décliner Width en 4/6/8 et recalculer les stamps intérieurs en conséquence.
    /// La forme de l'épaulière (Dome vs ConeUp, hauteur) est aussi un bon axe de variation.
    /// </summary>
    internal static class ArmGenerator
    {
        /// <summary>Largeur du gabarit capsule (boules d'épaule/coude/moufle).</summary>
        public const int Width = 6;

        /// <summary>Largeur de l'épaulière : déborde d'un voxel de chaque côté du bras.</summary>
        private const int PauldronWidth = 8;

        /// <summary>Rangées entre le centre de l'épaule et le centre du coude.</summary>
        private const int UpperLength = 3;

        public static BodyPart BuildUpperArm(
            CharacterPalette palette,
            CharacterSilhouette silhouette,
            float unit,
            int colorSeed
        )
        {
            // Grille élargie à PauldronWidth : la capsule verte reste centrée (marge de 1 de
            // chaque côté), l'épaulière occupe toute la largeur.
            var part = new VoxelPartMeshBuilder(
                PauldronWidth,
                9,
                PauldronWidth,
                new Vector3(4f, 5f, 4f),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Capsule : deux sphères pleines qui se chevauchent + colonne de liaison, le tout
            // dans la MÊME grille (le mesher ne sort que l'enveloppe, aucune face interne).
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(1, 0, 1),
                Width,
                6,
                Width,
                VoxelShape.Sphere,
                palette.OutfitPrimary
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(1, 2, 1),
                Width,
                6,
                Width,
                VoxelShape.Sphere,
                palette.OutfitPrimary
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 2, 2),
                Width - 2,
                3,
                Width - 2,
                VoxelShape.Column,
                palette.OutfitPrimary
            );

            // Épaulière : liseré de cuir clair pleine largeur au niveau de l'équateur de la
            // boule d'épaule, puis dôme de cuir sombre qui coiffe le tout — nettement plus
            // large que le bras, détachée visuellement de la manche verte. Absente pour les
            // silhouettes sans armure d'épaule (ex. squelette) : la boule reste nue.
            if (silhouette.HasPauldrons)
            {
                VoxelStamper.Stamp(
                    grid,
                    new Vector3Int(0, 5, 0),
                    PauldronWidth,
                    1,
                    PauldronWidth,
                    VoxelShape.Column,
                    palette.LeatherLight
                );
                VoxelStamper.Stamp(
                    grid,
                    new Vector3Int(0, 4, 0),
                    PauldronWidth,
                    5,
                    PauldronWidth,
                    VoxelShape.Dome,
                    palette.Belt
                );
            }

            Mesh mesh = part.ToMesh("CharacterUpperArm", colorSeed);

            return new BodyPart(
                CharacterRigDefinition.ArmL, // renommé au moment de l'assemblage (L/R)
                mesh,
                new PartSocket(
                    CharacterRigDefinition.SocketElbow,
                    part.VoxelToLocal(new Vector3(4f, 5f - UpperLength, 4f))
                )
            );
        }

        public static BodyPart BuildForearm(CharacterPalette palette, float unit, int colorSeed)
        {
            var part = new VoxelPartMeshBuilder(Width, 7, Width, new Vector3(3f, 5f, 3f), unit);
            VoxelGrid grid = part.Grid;

            // Bouchon de coude centré sur le pivot (plus étroit que la capsule du bras
            // supérieur : reste caché dedans), manche blanc cassé (contraste fort avec la
            // tunique verte et le cuir sombre), puis moufle de peau aplatie.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(1, 3, 1),
                Width - 2,
                4,
                Width - 2,
                VoxelShape.Sphere,
                palette.Pants
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(1, 2, 1),
                Width - 2,
                4,
                Width - 2,
                VoxelShape.Column,
                palette.Pants
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                Width,
                4,
                Width,
                VoxelShape.Sphere,
                palette.Skin
            );

            Mesh mesh = part.ToMesh("CharacterForearm", colorSeed);

            // Socket Grip : centre de la moufle, point où une arme/un outil tenu en main
            // (voir CharacterRigDefinition.WeaponGripAnchor) vient se poser pendant une
            // attaque/le minage.
            return new BodyPart(
                CharacterRigDefinition.ForearmL,
                mesh,
                new PartSocket(
                    CharacterRigDefinition.SocketGrip,
                    part.VoxelToLocal(new Vector3(3f, 1f, 3f))
                )
            );
        }
    }
}
