namespace CubeWorld.Core
{
    /// <summary>
    /// Stats RPG d'une entité (HP, MP, niveau, XP). Classe C# pure, sans
    /// dépendance à UnityEngine : composée par les MonoBehaviour propriétaires
    /// (PlayerHealth, EnemyHealth), jamais héritée.
    /// </summary>
    public sealed class StatBlock
    {
        // +10 MaxHP par niveau ; seuil d'XP = 100 + 50 par niveau déjà atteint.
        private const int HPGainPerLevel = 10;
        private const int BaseXPToNextLevel = 100;
        private const int XPGrowthPerLevel = 50;

        public int Level { get; private set; }
        public int CurrentXP { get; private set; }
        public int XPToNextLevel { get; private set; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentMP { get; private set; }
        public int MaxMP { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public StatBlock(int maxHP, int maxMP = 0, int level = 1)
        {
            Level = level;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            MaxMP = maxMP;
            CurrentMP = maxMP;
            XPToNextLevel = ComputeXPToNextLevel(Level);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHP = System.Math.Max(0, CurrentHP - amount);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHP = System.Math.Min(MaxHP, CurrentHP + amount);
        }

        /// <summary>Ajuste MaxHP (ex. équipement d'une armure) et soigne du même montant.</summary>
        public void IncreaseMaxHP(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            MaxHP += amount;
            CurrentHP = System.Math.Min(MaxHP, CurrentHP + amount);
        }

        /// <summary>Inverse de IncreaseMaxHP (ex. déséquipement d'une armure). MaxHP ne descend jamais sous 1.</summary>
        public void DecreaseMaxHP(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            MaxHP = System.Math.Max(1, MaxHP - amount);
            CurrentHP = System.Math.Min(CurrentHP, MaxHP);
        }

        /// <summary>
        /// Ajoute de l'XP et lève de niveau autant de fois que nécessaire (utile
        /// si un seul gain dépasse plusieurs seuils). Seul point qui publie
        /// <see cref="LevelUpEvent"/> : c'est le seul endroit qui sait quand un
        /// seuil est franchi.
        /// </summary>
        public void GainXP(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXP += amount;
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            MaxHP += HPGainPerLevel;
            CurrentHP = MaxHP;
            XPToNextLevel = ComputeXPToNextLevel(Level);

            EventBus.Publish(new LevelUpEvent(Level));
        }

        private static int ComputeXPToNextLevel(int level)
        {
            return BaseXPToNextLevel + (level - 1) * XPGrowthPerLevel;
        }
    }
}
