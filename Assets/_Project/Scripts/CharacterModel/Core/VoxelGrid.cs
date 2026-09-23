using UnityEngine;

namespace CubeWorld.CharacterModel.Core
{
    /// <summary>
    /// Grille voxel dense d'UNE pièce de personnage (tête, torse, bras...). Chaque cellule
    /// porte directement sa couleur ; alpha 0 = vide.
    ///
    /// C'est le renversement d'architecture par rapport à l'ancien PlayerVoxelMeshCore, qui
    /// meshait chaque primitive isolément (le masquage de faces ne voyait que les voisins de
    /// la même primitive, donc des faces internes cachées à chaque jointure) : ici on tamponne
    /// TOUTES les primitives d'une pièce dans la grille, PUIS on meshe la grille une seule
    /// fois (<see cref="VoxelGridMesher"/>) — le culling de faces voit la pièce entière.
    /// </summary>
    internal sealed class VoxelGrid
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        private readonly Color32[,,] cells;

        public VoxelGrid(int sizeX, int sizeY, int sizeZ)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            cells = new Color32[sizeX, sizeY, sizeZ];
        }

        public bool InBounds(int x, int y, int z) =>
            x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ;

        /// <summary>Hors grille = vide : les faces en bord de grille sont toujours émises.</summary>
        public bool IsSolid(int x, int y, int z) => InBounds(x, y, z) && cells[x, y, z].a != 0;

        public Color32 Get(int x, int y, int z) => cells[x, y, z];

        /// <summary>Écrit la couleur (alpha forcé opaque). Les écritures hors grille sont ignorées.</summary>
        public void Set(int x, int y, int z, Color32 color)
        {
            if (!InBounds(x, y, z))
            {
                return;
            }

            color.a = 255;
            cells[x, y, z] = color;
        }

        public void Clear(int x, int y, int z)
        {
            if (InBounds(x, y, z))
            {
                cells[x, y, z] = default;
            }
        }

        /// <summary>
        /// Recolore le voxel le plus avancé (face +Z = avant du personnage) de la colonne
        /// (x, y). Sert à peindre des motifs sur la surface avant d'une pièce (emblème,
        /// boucle de ceinture, liserés) sans connaître sa forme exacte.
        /// </summary>
        public void PaintFrontmost(int x, int y, Color32 color)
        {
            for (int z = SizeZ - 1; z >= 0; z--)
            {
                if (IsSolid(x, y, z))
                {
                    Set(x, y, z, color);
                    return;
                }
            }
        }
    }
}
