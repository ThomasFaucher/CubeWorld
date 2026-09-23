namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>
    /// Traduit une <see cref="CharacterExpression"/> en paramètres de dessin du visage
    /// (largeur des yeux/bouche, décalage de la bouche, sourcils). Consommé par le
    /// générateur de tête au moment de peindre les voxels frontaux.
    /// </summary>
    internal readonly struct ExpressionShape
    {
        public readonly bool EyesClosed;
        public readonly float EyeWidthMul;
        public readonly float MouthWidthMul;
        public readonly int MouthRowOffset;
        public readonly bool EyebrowsAngry;
        public readonly bool EyebrowsSad;

        private ExpressionShape(
            bool eyesClosed,
            float eyeWidthMul,
            float mouthWidthMul,
            int mouthRowOffset,
            bool eyebrowsAngry,
            bool eyebrowsSad
        )
        {
            EyesClosed = eyesClosed;
            EyeWidthMul = eyeWidthMul;
            MouthWidthMul = mouthWidthMul;
            MouthRowOffset = mouthRowOffset;
            EyebrowsAngry = eyebrowsAngry;
            EyebrowsSad = eyebrowsSad;
        }

        public static ExpressionShape For(CharacterExpression expression) =>
            expression switch
            {
                CharacterExpression.Happy => new ExpressionShape(
                    false,
                    0.9f,
                    1.7f,
                    1,
                    false,
                    false
                ),
                CharacterExpression.Angry => new ExpressionShape(false, 0.8f, 0.9f, 0, true, false),
                CharacterExpression.Sad => new ExpressionShape(false, 0.9f, 0.7f, -1, false, true),
                CharacterExpression.Surprised => new ExpressionShape(
                    false,
                    1.6f,
                    0.4f,
                    0,
                    false,
                    false
                ),
                CharacterExpression.Blink => new ExpressionShape(true, 1f, 1f, 0, false, false),
                _ => new ExpressionShape(false, 1f, 1f, 0, false, false),
            };
    }
}
