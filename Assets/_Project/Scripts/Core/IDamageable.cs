namespace CubeWorld.Core
{
    /// <summary>
    /// Implémenté par toute entité capable d'encaisser des dégâts (joueur,
    /// ennemis). Chaque implémentation compose son propre <see cref="StatBlock"/> ;
    /// pas de classe de base commune, pour laisser Player et Combat indépendants.
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }

        void ApplyDamage(DamageInfo info);
    }
}
