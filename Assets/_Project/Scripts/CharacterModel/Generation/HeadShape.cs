using UnityEngine;

namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>
    /// Profil de tête : largeur de chaque rangée (menton -> sommet) + rangées repères du
    /// visage. La tête est construite comme un empilement de sections de ces largeurs.
    ///
    /// Direction artistique : tête RECTANGULAIRE (façon Link chibi) — les flancs restent
    /// quasi verticaux du menton jusqu'à près du sommet (léger renflement aux joues), puis
    /// le crâne se referme rapidement avec des coins adoucis. Combiné aux sections
    /// "squircle" du générateur (carré à coins arrondis au lieu de disques), ça donne une
    /// boîte douce plutôt qu'une boule.
    /// </summary>
    internal sealed class HeadShape
    {
        /// <summary>Largeur = profondeur de la tête en voxels.</summary>
        public int HeadWidth { get; }

        public int[] RowWidth { get; }
        public int MouthRow { get; }
        public int BlushRow { get; }
        public int EyeRow { get; }

        /// <summary>Première rangée couverte par le bonnet (racine des cheveux 2 rangées sous elle).</summary>
        public int HairRowStart { get; }

        private HeadShape(
            int headWidth,
            int[] rowWidth,
            int mouthRow,
            int blushRow,
            int eyeRow,
            int hairRowStart
        )
        {
            HeadWidth = headWidth;
            RowWidth = rowWidth;
            MouthRow = mouthRow;
            BlushRow = blushRow;
            EyeRow = eyeRow;
            HairRowStart = hairRowStart;
        }

        public static HeadShape Generate(int headWidth, int rows, CharacterRng rng)
        {
            // Fractions de largeur par zone (par rapport à headWidth), variées par seed dans
            // des fourchettes resserrées. Menton presque aussi large que les joues et crâne
            // qui reste large jusqu'à skullT : c'est ce qui rend la silhouette rectangulaire.
            float chinFrac = rng.NextFloat(0.78f, 0.84f);
            float cheekFrac = rng.NextFloat(0.88f, 0.94f);
            float skullFrac = rng.NextFloat(0.86f, 0.92f);
            float topFrac = rng.NextFloat(0.45f, 0.55f);
            float cheekT = rng.NextFloat(0.22f, 0.30f);
            float skullT = rng.NextFloat(0.82f, 0.88f);

            var rowWidth = new int[rows];

            for (int row = 0; row < rows; row++)
            {
                float t = rows <= 1 ? 0f : (float)row / (rows - 1);
                float widthFrac;

                if (t < cheekT)
                {
                    // Menton -> joues : montée courte et douce.
                    widthFrac = Mathf.Lerp(
                        chinFrac,
                        cheekFrac,
                        Mathf.SmoothStep(0f, 1f, t / cheekT)
                    );
                }
                else if (t < skullT)
                {
                    // Joues -> haut du crâne : quasi plat = flancs verticaux (le rectangle).
                    widthFrac = Mathf.Lerp(cheekFrac, skullFrac, (t - cheekT) / (skullT - cheekT));
                }
                else
                {
                    // Fermeture rapide sur les dernières rangées, coins adoucis au cosinus.
                    float tt = (t - skullT) / (1f - skullT);
                    widthFrac = Mathf.Lerp(
                        skullFrac,
                        topFrac,
                        1f - Mathf.Cos(tt * Mathf.PI * 0.5f)
                    );
                }

                rowWidth[row] = Mathf.Max(2, Mathf.RoundToInt(widthFrac * headWidth));
            }

            // Rangées repères du visage, en fraction de la hauteur (bas -> haut).
            int mouthRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.15f), 0, rows - 1);
            int blushRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.30f), 0, rows - 1);
            int eyeRow = Mathf.Clamp(Mathf.RoundToInt(rows * 0.46f), 0, rows - 1);
            int hairRowStart = Mathf.Clamp(Mathf.RoundToInt(rows * 0.78f), 0, rows - 1);

            return new HeadShape(headWidth, rowWidth, mouthRow, blushRow, eyeRow, hairRowStart);
        }
    }
}
