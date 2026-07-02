namespace CubeWorld.Core
{
    /// <summary>Annonce qu'un gain d'XP est disponible (publié par EnemyHealth à la mort). Le joueur s'y abonne et l'applique à son propre StatBlock.</summary>
    public readonly struct XPGainedEvent : IGameEvent
    {
        public readonly int Amount;

        public XPGainedEvent(int amount)
        {
            Amount = amount;
        }
    }
}
