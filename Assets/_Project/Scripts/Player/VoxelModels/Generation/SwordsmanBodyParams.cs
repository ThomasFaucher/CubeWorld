namespace CubeWorld.Player.VoxelModels.Generation
{
    internal enum PauldronStyle
    {
        Spiky,
        Round,
    }

    internal enum HairStyle
    {
        SidePartAhoge,
        Mohawk,
        Slick,
    }

    /// <summary>
    /// Choix de silhouette/accessoires du Swordsman générés par seed, dans des fourchettes
    /// resserrées pour que la silhouette reste reconnaissable d'une variante à l'autre.
    /// </summary>
    internal readonly struct SwordsmanBodyParams
    {
        public readonly int HeadSize;
        public readonly float TorsoWidthScale;
        public readonly float ArmThicknessScale;
        public readonly bool HasCape;
        public readonly PauldronStyle Pauldron;
        public readonly HairStyle Hair;

        private SwordsmanBodyParams(
            int headSize,
            float torsoWidthScale,
            float armThicknessScale,
            bool hasCape,
            PauldronStyle pauldron,
            HairStyle hair
        )
        {
            HeadSize = headSize;
            TorsoWidthScale = torsoWidthScale;
            ArmThicknessScale = armThicknessScale;
            HasCape = hasCape;
            Pauldron = pauldron;
            Hair = hair;
        }

        public static SwordsmanBodyParams Generate(CharacterRng rng)
        {
            // Tête volontairement énorme (silhouette chibi très marquée) : associée au corps
            // trapu de SwordsmanVoxelModel, elle pèse ~45% de la hauteur totale du personnage.
            int headSize = rng.NextInt(18, 23);
            float torsoWidthScale = rng.NextFloat(0.92f, 1.08f);
            float armThicknessScale = rng.NextFloat(0.92f, 1.08f);
            bool hasCape = rng.NextBool(0.7f);
            var pauldron = rng.Pick(new[] { PauldronStyle.Spiky, PauldronStyle.Round });
            var hair = rng.Pick(new[] { HairStyle.SidePartAhoge, HairStyle.Mohawk, HairStyle.Slick });

            return new SwordsmanBodyParams(headSize, torsoWidthScale, armThicknessScale, hasCape, pauldron, hair);
        }
    }
}
