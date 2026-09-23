using UnityEngine;

namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>Variante de bonnet/coiffure du héros (voir HeadGenerator pour le rendu).</summary>
    internal enum HairStyle
    {
        /// <summary>Bonnet pointu incliné vers l'arrière + frange droite.</summary>
        Classic,

        /// <summary>Bonnet plus haut et plus droit.</summary>
        TallCap,

        /// <summary>Bonnet classique + mèche balayée sur un côté du front.</summary>
        SideSwept,
    }

    /// <summary>
    /// Proportions d'un personnage, tirées par seed dans des fourchettes resserrées : la
    /// silhouette chibi (tête ~45% de la hauteur, membres épais) reste reconnaissable d'un
    /// seed à l'autre, seuls les gabarits fins varient.
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : élargir/ajouter des fourchettes ici (et les
    /// consommer dans les générateurs de pièces) pour différencier davantage les personnages
    /// — ex. LegLengthScale, taille des oreilles, accessoires de dos...
    /// </summary>
    internal readonly struct CharacterBodyParams
    {
        /// <summary>Largeur de la tête en voxels (aussi sa profondeur).</summary>
        public readonly int HeadWidth;

        /// <summary>Nombre de rangées de la tête (menton -> sommet du crâne).</summary>
        public readonly int HeadRows;

        public readonly float TorsoWidthScale;

        /// <summary>Épaisseur commune bras/jambes (membres épais = style chibi).</summary>
        public readonly float LimbThicknessScale;

        public readonly HairStyle Hair;

        private CharacterBodyParams(
            int headWidth,
            int headRows,
            float torsoWidthScale,
            float limbThicknessScale,
            HairStyle hair
        )
        {
            HeadWidth = headWidth;
            HeadRows = headRows;
            TorsoWidthScale = torsoWidthScale;
            LimbThicknessScale = limbThicknessScale;
            Hair = hair;
        }

        public static CharacterBodyParams Generate(CharacterRng rng)
        {
            // Tête grosse mais pas envahissante (~1/3 de la hauteur hors bonnet), un peu
            // moins haute que large : la boîte rectangulaire lit mieux légèrement écrasée.
            int headWidth = rng.NextInt(14, 18);
            int headRows = Mathf.Max(11, Mathf.RoundToInt(headWidth * 0.82f));

            float torsoWidthScale = rng.NextFloat(0.92f, 1.1f);
            float limbThicknessScale = rng.NextFloat(0.9f, 1.15f);
            var hair = rng.Pick(
                new[] { HairStyle.Classic, HairStyle.TallCap, HairStyle.SideSwept }
            );

            return new CharacterBodyParams(
                headWidth,
                headRows,
                torsoWidthScale,
                limbThicknessScale,
                hair
            );
        }
    }
}
