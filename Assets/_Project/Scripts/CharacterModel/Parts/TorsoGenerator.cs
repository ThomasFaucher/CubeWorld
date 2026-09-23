using CubeWorld.CharacterModel.Core;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel.Parts
{
    /// <summary>
    /// Buste, en trois silhouettes possibles (<see cref="TorsoStyle"/>) : tunique en tonneau
    /// (taille resserrée + buste plein + col + BAUDRIER de cuir en diagonale, signature de
    /// l'épéiste/l'elfe), cage thoracique à claire-voie (bandes de côtes séparées de vrais
    /// trous + colonne vertébrale, squelette), ou robe ample sans taille resserrée avec col en
    /// V et cordelette centrale (mage). Pivot au bas du torse (posé sur le pelvis). Expose Neck
    /// (le cou de la tête s'y encastre d'une rangée) et ShoulderL/ShoulderR (centres des
    /// boules d'épaule des bras) — ces sockets sont purement géométriques, indépendants du
    /// remplissage de la grille, donc identiques dans les trois styles.
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
            CharacterSilhouette silhouette,
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

            switch (silhouette.Torso)
            {
                case TorsoStyle.Ribcage:
                    StampRibcage(grid, width, height, depth, palette);
                    break;
                case TorsoStyle.Robe:
                    StampRobe(grid, width, height, depth, palette);
                    break;
                default:
                    StampSolidTorso(grid, width, height, depth, palette);
                    break;
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
                // Deuxième port dans le dos (outil, ex. pioche) : même hauteur/profondeur que
                // SocketBack, mais incliné à l'opposé (-35° au lieu de +35°) pour lire comme
                // deux objets croisés plutôt que superposés.
                new PartSocket(
                    CharacterRigDefinition.SocketBackTool,
                    part.VoxelToLocal(new Vector3(width / 2f, height, -2f)),
                    Quaternion.Euler(0f, 0f, -35f)
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

        /// <summary>Buste plein "tonneau" du héros : taille + buste + col + baudrier diagonal.</summary>
        private static void StampSolidTorso(
            VoxelGrid grid,
            int width,
            int height,
            int depth,
            CharacterPalette palette
        )
        {
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
        }

        /// <summary>
        /// Robe ample du mage : pas de taille resserrée façon tonneau (silhouette qui tombe
        /// droit du col aux hanches), col en V ouvert au lieu du col fermé + baudrier
        /// diagonal martial de l'épéiste, et une cordelette centrale avec fermoir (accent)
        /// en guise de ceinture.
        /// </summary>
        private static void StampRobe(
            VoxelGrid grid,
            int width,
            int height,
            int depth,
            CharacterPalette palette
        )
        {
            // Colonne pleine largeur du bas jusqu'au col : contrairement au tonneau de
            // l'épéiste, aucun décroché de taille — c'est ce qui lit comme une robe qui tombe
            // plutôt qu'une tunique cintrée.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(0, 0, 0),
                width,
                height,
                depth,
                VoxelShape.Column,
                palette.OutfitPrimary
            );

            // Col en V ouvert sur le buste : bande qui s'évase en descendant, peinte sur la
            // surface avant seulement (contraste avec le col fermé pleine largeur du héros).
            int collarRows = 3;
            for (int i = 0; i < collarRows; i++)
            {
                int row = height - 1 - i;
                float vHalf = Mathf.Max(1f, 0.6f + i * 1.1f);
                int minX = Mathf.RoundToInt((width / 2f) - vHalf);
                int maxX = Mathf.RoundToInt((width / 2f) + vHalf);

                for (int x = minX; x <= maxX; x++)
                {
                    grid.PaintFrontmost(x, row, palette.OutfitTrim);
                }
            }

            // Cordelette centrale + fermoir : unique touche de couleur vive (accent), en
            // écho au baudrier de l'épéiste mais verticale et sans connotation martiale.
            int cordX = width / 2;
            for (int y = 1; y < height - collarRows; y++)
            {
                grid.PaintFrontmost(cordX, y, palette.Belt);
            }
            grid.PaintFrontmost(cordX, height / 2, palette.Emblem);
        }

        /// <summary>
        /// Cage thoracique à claire-voie du squelette : anneau bas (cache le plug du pelvis,
        /// identique au style plein), colonne vertébrale qui relie tout, bandes de côtes
        /// pleine largeur séparées de rangées VOLONTAIREMENT NON stampées (donc vides, alpha
        /// 0) — un vrai trou traversant vu de face comme de profil, pas un motif peint — puis
        /// clavicules pleines en haut pour un socket Neck propre.
        /// </summary>
        private static void StampRibcage(
            VoxelGrid grid,
            int width,
            int height,
            int depth,
            CharacterPalette palette
        )
        {
            // Anneau bas : cache le plug du pelvis, comme dans le style plein.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, 0, 1),
                width - 4,
                3,
                depth - 2,
                VoxelShape.Column,
                palette.OutfitSecondary
            );

            // Colonne vertébrale : tige fine à l'arrière qui relie les côtes entre elles —
            // sans elle, les bandes séparées par les trous flotteraient sans lien visuel.
            int spineWidth = 2;
            int spineX = (width - spineWidth) / 2;
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(spineX, 3, 0),
                spineWidth,
                height - 3,
                2,
                VoxelShape.Column,
                palette.OutfitPrimary
            );

            // Côtes : une rangée pleine sur deux entre la taille et les clavicules ; la
            // rangée "sautée" reste à alpha 0 dans la grille = trou réel, pas un aplat peint.
            for (int row = 3; row <= height - 2; row++)
            {
                bool isRib = (row - 3) % 2 == 0;
                if (!isRib)
                {
                    continue;
                }

                VoxelStamper.Stamp(
                    grid,
                    new Vector3Int(0, row, 0),
                    width,
                    1,
                    depth,
                    VoxelShape.Column,
                    palette.OutfitPrimary
                );
            }

            // Clavicules : rangée du haut pleine, encastrée dans l'encolure comme le col du
            // héros — nécessaire pour un socket Neck propre.
            VoxelStamper.Stamp(
                grid,
                new Vector3Int(2, height - 1, 1),
                width - 4,
                1,
                depth - 2,
                VoxelShape.Column,
                palette.OutfitTrim
            );
        }
    }
}
