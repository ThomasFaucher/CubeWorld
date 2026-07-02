using CubeWorld.Core;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Santé et progression du joueur : encaisse les dégâts, gagne l'XP
    /// publiée à la mort des ennemis (voir Combat.EnemyHealth).
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats de départ")]
        [SerializeField]
        private int _maxHP = 100;

        [SerializeField]
        private int _maxMP = 20;

        private StatBlock stats;
        private PlayerEquipment equipment;

        public bool IsDead => stats.IsDead;
        public StatBlock Stats => stats;

        private void Awake()
        {
            stats = new StatBlock(_maxHP, _maxMP);
        }

        /// <summary>Branche l'équipement dont les bonus/réductions affectent cette santé. Appelé par PlayerBootstrap.</summary>
        public void BindEquipment(PlayerEquipment playerEquipment)
        {
            equipment = playerEquipment;
        }

        /// <summary>Ajuste MaxHP suite à un équipement/déséquipement d'armure (delta négatif = retrait).</summary>
        public void ApplyArmorBonus(int delta)
        {
            if (delta > 0)
            {
                stats.IncreaseMaxHP(delta);
            }
            else if (delta < 0)
            {
                stats.DecreaseMaxHP(-delta);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<XPGainedEvent>(OnXPGained);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<XPGainedEvent>(OnXPGained);
        }

        public void ApplyDamage(DamageInfo info)
        {
            if (stats.IsDead)
            {
                return;
            }

            int defense = equipment != null ? equipment.TotalDefense : 0;
            int reducedAmount = Mathf.Max(0, info.Amount - defense);

            stats.ApplyDamage(reducedAmount);
            EventBus.Publish(new DamageDealtEvent(gameObject, reducedAmount, stats.CurrentHP));
        }

        private void OnXPGained(XPGainedEvent evt)
        {
            stats.GainXP(evt.Amount);
        }
    }
}
