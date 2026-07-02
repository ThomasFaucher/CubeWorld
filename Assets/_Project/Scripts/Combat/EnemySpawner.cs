using System.Collections.Generic;
using CubeWorld.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Fait apparaître des ennemis dans la zone NavMesh bakée (voir
    /// World.NavMeshRegionBaker) jusqu'à un plafond, et détruit ceux qui se
    /// retrouvent hors de la nouvelle zone bakée — piggy-back sur le même
    /// event que le bake pour le nettoyage au déchargement de chunks, sans
    /// plomberie séparée.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField]
        private EnemyDefinition _enemyDefinition;

        [Header("Spawn")]
        [SerializeField]
        private int _maxAlive = 6;

        [Tooltip("Rayon autour du joueur dans lequel aucun ennemi ne peut apparaître.")]
        [SerializeField]
        private float _minDistanceFromPlayer = 8f;

        [SerializeField]
        private int _spawnAttemptsPerBake = 4;

        private readonly List<EnemyHealth> alive = new();

        private void OnEnable()
        {
            EventBus.Subscribe<NavMeshBakedEvent>(OnNavMeshBaked);
            EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NavMeshBakedEvent>(OnNavMeshBaked);
            EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            alive.RemoveAll(enemy => enemy == null || enemy.gameObject == evt.Entity);
        }

        private void OnNavMeshBaked(NavMeshBakedEvent evt)
        {
            var bounds = new Bounds(evt.Center, evt.Size);

            CleanupOutsideBounds(bounds);
            SpawnWithinBounds(evt.Center, bounds);
        }

        private void CleanupOutsideBounds(Bounds bounds)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null)
                {
                    alive.RemoveAt(i);
                    continue;
                }

                if (!bounds.Contains(enemy.transform.position))
                {
                    alive.RemoveAt(i);
                    Destroy(enemy.gameObject);
                }
            }
        }

        private void SpawnWithinBounds(Vector3 center, Bounds bounds)
        {
            if (_enemyDefinition == null)
            {
                return;
            }

            Transform player = PlayerContext.Transform;

            for (
                int attempt = 0;
                attempt < _spawnAttemptsPerBake && alive.Count < _maxAlive;
                attempt++
            )
            {
                Vector3 randomPoint =
                    center
                    + new Vector3(
                        Random.Range(-bounds.extents.x, bounds.extents.x),
                        0f,
                        Random.Range(-bounds.extents.z, bounds.extents.z)
                    );

                if (
                    !NavMesh.SamplePosition(
                        randomPoint,
                        out NavMeshHit hit,
                        bounds.extents.y,
                        NavMesh.AllAreas
                    )
                )
                {
                    continue;
                }

                if (
                    player != null
                    && Vector3.Distance(hit.position, player.position) < _minDistanceFromPlayer
                )
                {
                    continue;
                }

                SpawnEnemy(hit.position);
            }
        }

        private void SpawnEnemy(Vector3 position)
        {
            var enemyObject = new GameObject($"Enemy_{_enemyDefinition.DisplayName}");
            enemyObject.transform.position = position;

            // AddComponent<EnemyAI> ajoute aussi NavMeshAgent (RequireComponent).
            var ai = enemyObject.AddComponent<EnemyAI>();
            var health = enemyObject.AddComponent<EnemyHealth>();

            ai.Initialize(_enemyDefinition);
            health.Initialize(_enemyDefinition);

            alive.Add(health);
        }
    }
}
