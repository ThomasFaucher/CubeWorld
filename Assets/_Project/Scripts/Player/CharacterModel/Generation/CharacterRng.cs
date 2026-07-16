using System;

namespace CubeWorld.Player.CharacterModel.Generation
{
    /// <summary>Source aléatoire unique d'un personnage généré : même seed -> même résultat.</summary>
    internal sealed class CharacterRng
    {
        private readonly Random random;

        public CharacterRng(int seed)
        {
            random = new Random(seed);
        }

        public float NextFloat(float min, float max) =>
            min + ((float)random.NextDouble() * (max - min));

        public int NextInt(int minInclusive, int maxExclusive) =>
            random.Next(minInclusive, maxExclusive);

        public bool NextBool(float chanceOfTrue) => random.NextDouble() < chanceOfTrue;

        public T Pick<T>(T[] options) => options[random.Next(options.Length)];
    }
}
