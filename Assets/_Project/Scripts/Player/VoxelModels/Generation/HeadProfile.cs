using UnityEngine;

namespace CubeWorld.Player.VoxelModels.Generation
{
    /// <summary>
    /// Généralise les tableaux HeadRowWidth/HeadRowFrontInset historiques du Swordsman en une
    /// courbe procédurale : largeur/retrait par rangée (bas -> haut), plus les rangées repères
    /// (yeux/bouche/joues/racine des cheveux) exprimées en fraction de la hauteur, comme le fait
    /// déjà PlayerVoxelCuteFace pour les têtes sphériques.
    /// </summary>
    internal sealed class HeadProfile
    {
        public int HeadSize { get; }
        public int[] RowWidth { get; }
        public int[] RowFrontInset { get; }
        public int MouthRow { get; }
        public int BlushRow { get; }
        public int EyeRow { get; }
        public int HairRowStart { get; }

        private HeadProfile(
            int headSize,
            int[] rowWidth,
            int[] rowFrontInset,
            int mouthRow,
            int blushRow,
            int eyeRow,
            int hairRowStart
        )
        {
            HeadSize = headSize;
            RowWidth = rowWidth;
            RowFrontInset = rowFrontInset;
            MouthRow = mouthRow;
            BlushRow = blushRow;
            EyeRow = eyeRow;
            HairRowStart = hairRowStart;
        }

        public static HeadProfile Generate(int headSize, int rows, CharacterRng rng)
        {
            // Fractions de largeur (par rapport à headSize) qui rejouent la silhouette
            // historique du Swordsman (mâchoire ~0.375, joues ~0.75, crâne ~0.125),
            // avec une petite variation par seed. Mâchoire élargie (~0.5-0.58) pour un menton
            // presque aussi large que les joues : silhouette "bébé" ronde façon chibi, sans le
            // menton pointu qu'un jawWidthFrac beaucoup plus petit que cheekWidthFrac donnerait.
            float jawWidthFrac = rng.NextFloat(0.50f, 0.58f);
            float cheekWidthFrac = rng.NextFloat(0.70f, 0.80f);
            float skullWidthFrac = rng.NextFloat(0.10f, 0.16f);
            float cheekPeakT = rng.NextFloat(0.28f, 0.40f);
            float maxInsetFrac = rng.NextFloat(0.28f, 0.34f);

            var rowWidth = new int[rows];
            var rowFrontInset = new int[rows];

            for (int row = 0; row < rows; row++)
            {
                float t = rows <= 1 ? 0f : (float)row / (rows - 1);

                float widthFrac = t < cheekPeakT
                    ? Mathf.Lerp(jawWidthFrac, cheekWidthFrac, t / cheekPeakT)
                    : Mathf.Lerp(cheekWidthFrac, skullWidthFrac, (t - cheekPeakT) / (1f - cheekPeakT));

                float distanceFromPeak = Mathf.Abs(t - cheekPeakT) / Mathf.Max(cheekPeakT, 1f - cheekPeakT);

                rowWidth[row] = Mathf.Max(2, Mathf.RoundToInt(widthFrac * headSize));
                rowFrontInset[row] = Mathf.Max(0, Mathf.RoundToInt(distanceFromPeak * maxInsetFrac * headSize));
            }

            // Rangées repères en fraction de la hauteur, cohérentes avec les fractions
            // utilisées par PlayerVoxelCuteFace pour les têtes sphériques des autres archétypes.
            int mouthRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.13f), 0, rows - 1);
            int blushRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.27f), 0, rows - 1);
            int eyeRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.47f), 0, rows - 1);
            int hairRowStart = Mathf.Clamp(Mathf.RoundToInt(rows * 0.80f), 0, rows - 1);

            return new HeadProfile(headSize, rowWidth, rowFrontInset, mouthRow, blushRow, eyeRow, hairRowStart);
        }
    }
}
