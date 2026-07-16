using CubeWorld.Player.CharacterModel.Core;
using CubeWorld.Player.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Parts
{
    /// <summary>
    /// Buste : tunique verte en tonneau (taille sombre + buste large), col crème, et
    /// BAUDRIER de cuir peint en diagonale sur la poitrine (l'attache d'épée dans le dos,
    /// signature du héros). Pivot au bas du torse (posé sur le pelvis). Expose Neck (le cou
    /// de la tête s'y encastre d'une rangée) et ShoulderL/ShoulderR (centres des boules
    /// d'épaule des bras).
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : les motifs peints sur la surface avant
    /// (baudrier, broderies, laçage...) sont un bon endroit pour différencier les tenues
    /// par seed sans changer la silhouette.
    /// </summary>
    internal static class TorsoGenerator
    {
        public static BodyPart Build(
            int width,
            int height,
            int depth,
            int armWidth,
            CharacterPalette palette,
            float unit,
            int colorSeed
        )
        {
            var part = new VoxelPartMeshBuilder(
                width,
                height,
                depth,
                new Vector3(width / 2f, 0f, depth / 2f),
                unit
            );
            VoxelGrid grid = part.Grid;

            // Taille resserrée (vert sombre) puis buste pleine largeur : le décroché crée la
            // silhouette "tonneau" et laisse la ceinture du pelvis dépasser en anneau.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 0, 1),
                width - 4,
                3,
                depth - 2,
                VoxelShape.Column,
                palette.OutfitSecondary
            );
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 3, 0),
                width,
                height - 3,
                depth,
                VoxelShape.Column,
                palette.OutfitPrimary
            );

            // Col crème encastré dans l'encolure (rangée du haut).
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, height - 1, 1),
                width - 4,
                1,
                depth - 2,
                VoxelShape.Column,
                palette.OutfitTrim
            );

            // Baudrier : bande de cuir peinte en diagonale sur la poitrine, de l'épaule
            // droite du personnage vers la hanche gauche.
            for (int y = 0; y < height - 1; y++)
            {
                float t = (float)y / (height - 1);
                int strapX = Mathf.RoundToInt(
                    (width / 2f) - 0.5f + Mathf.Lerp(-width * 0.26f, width * 0.26f, t)
                );
                grid.PaintFrontmost(strapX, y, palette.Belt);
                grid.PaintFrontmost(strapX + 1, y, palette.Belt);
            }

            Mesh mesh = part.ToMesh("CharacterTorso", colorSeed);

            // Épaules : centre de la boule d'épaule du bras, à moitié encastrée dans le buste
            // (le décalage en demi-voxel désaligne les grilles bras/torse : aucune face
            // coplanaire possible entre les deux pièces).
            float shoulderX = (width / 2f) + (armWidth / 2f) - 1.5f;

            return new BodyPart(
                CharacterRigDefinition.Torso,
                mesh,
                new PartSocket(
                    CharacterRigDefinition.SocketNeck,
                    part.VoxelToLocal(new Vector3(width / 2f, height - 1, depth / 2f))
                ),
                // Dos : point de port de l'épée — 2 voxels derrière la surface arrière
                // (l'épée fait 3 d'épaisseur autour de son pivot : elle flotte à ~0.5 voxel
                // du dos, jamais encastrée), garde à hauteur d'épaule, inclinée en diagonale.
                new PartSocket(
                    CharacterRigDefinition.SocketBack,
                    part.VoxelToLocal(new Vector3(width / 2f, height, -2f)),
                    Quaternion.Euler(0f, 0f, 35f)
                ),
                new PartSocket(
                    CharacterRigDefinition.SocketShoulderL,
                    part.VoxelToLocal(new Vector3((width / 2f) - shoulderX, height - 2, depth / 2f))
                ),
                new PartSocket(
                    CharacterRigDefinition.SocketShoulderR,
                    part.VoxelToLocal(new Vector3((width / 2f) + shoulderX, height - 2, depth / 2f))
                )
            );
        }
    }
}
