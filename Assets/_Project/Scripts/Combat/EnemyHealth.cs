using CubeWorld.Core;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Santé d'un ennemi : StatBlock construit depuis son EnemyDefinition,
    /// publie les events de dégâts/mort, tire la table de loot à la mort.
    /// Initialize() est appelé par EnemySpawner juste après AddComponent (pas
    /// de config via l'inspecteur : les ennemis sont instanciés en code).
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        private EnemyDefinition definition;
        private StatBlock stats;

        public bool IsDead => stats == null || stats.IsDead;

        public void Initialize(EnemyDefinition enemyDefinition)
        {
            definition = enemyDefinition;
            stats = new StatBlock(definition.MaxHP);
        }

        public void ApplyDamage(DamageInfo info)
        {
            if (stats == null || stats.IsDead)
            {
                return;
            }

            stats.ApplyDamage(info.Amount);
            EventBus.Publish(new DamageDealtEvent(gameObject, info.Amount, stats.CurrentHP));

            if (stats.IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            EventBus.Publish(new EntityDiedEvent(gameObject));
            EventBus.Publish(new XPGainedEvent(definition.XPReward));

            SpawnLoot();

            Destroy(gameObject);
        }

        private void SpawnLoot()
        {
            LootTableDefinition lootTable = definition.LootTable;
            if (lootTable == null)
            {
                return;
            }

            foreach (LootTableDefinition.LootEntry entry in lootTable.Entries)
            {
                if (entry.Item == null || Random.value > entry.DropChance)
                {
                    continue;
                }

                int quantity = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                if (quantity <= 0)
                {
                    continue;
                }

                ItemPickup.Spawn(entry.Item, quantity, transform.position);
            }
        }
    }
}
