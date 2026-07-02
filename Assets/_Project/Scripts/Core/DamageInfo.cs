namespace CubeWorld.Core
{
    /// <summary>Description minimale d'un coup infligé. Volontairement réduite au strict nécessaire.</summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;

        public DamageInfo(int amount)
        {
            Amount = amount;
        }
    }
}
