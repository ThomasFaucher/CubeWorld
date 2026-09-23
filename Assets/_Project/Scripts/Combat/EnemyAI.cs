using CubeWorld.CharacterModel;
using CubeWorld.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CubeWorld.Combat
{
    /// <summary>
    /// IA d'un ennemi : décision throttlée (EnemyAIBrain) qui pilote un
    /// NavMeshAgent (Idle/Chase/Attack). Initialize() est appelé par
    /// EnemySpawner juste après AddComponent — les ennemis sont instanciés en
    /// code, pas de config via l'inspecteur.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private const float DecisionInterval = 0.2f;

        private EnemyDefinition definition;
        private NavMeshAgent agent;
        private readonly EnemyAIBrain brain = new();
        private readonly MeleeAttackResolver attackResolver = new();

        private float decisionTimer;
        private float attackTimer;

        public void Initialize(EnemyDefinition enemyDefinition)
        {
            definition = enemyDefinition;

            agent = GetComponent<NavMeshAgent>();
            agent.speed = definition.MoveSpeed;
            agent.stoppingDistance = definition.AttackRange * 0.9f;
            agent.radius = definition.Radius;
            agent.height = definition.Height;

            var hitCollider = gameObject.AddComponent<CapsuleCollider>();
            hitCollider.height = definition.Height;
            hitCollider.radius = definition.Radius;
            hitCollider.center = new Vector3(0f, definition.Height * 0.5f, 0f);

            CreateVisual();
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            KeepOutOfPlayer();

            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0f)
            {
                return;
            }

            decisionTimer = DecisionInterval;
            Decide();
        }

        private void Decide()
        {
            Transform target = PlayerContext.Transform;
            if (target == null)
            {
                agent.ResetPath();
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);
            EnemyAction action = brain.Decide(
                distance,
                definition.DetectionRadius,
                definition.AttackRange
            );

            switch (action)
            {
                case EnemyAction.Idle:
                    agent.ResetPath();
                    break;

                case EnemyAction.Chase:
                    agent.SetDestination(ApproachPoint(target.position));
                    break;

                case EnemyAction.Attack:
                    agent.ResetPath();
                    FaceTarget(target.position);
                    TryAttack();
                    break;
            }
        }

        // Point d'arrivée devant le joueur (côté ennemi), à distance d'attaque : viser son
        // centre pousserait l'agent contre sa capsule, que le NavMeshAgent ne voit pas.
        private Vector3 ApproachPoint(Vector3 targetPosition)
        {
            Vector3 fromTarget = transform.position - targetPosition;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude < 0.0001f)
            {
                return targetPosition;
            }

            return targetPosition + fromTarget.normalized * agent.stoppingDistance;
        }

        // Le NavMeshAgent n'évite que les autres agents, pas le CharacterController du
        // joueur : si les deux capsules se chevauchent, on repousse l'ennemi (agent.Move
        // le garde sur le NavMesh). Fait chaque frame, pas seulement au tick de décision.
        private void KeepOutOfPlayer()
        {
            Transform target = PlayerContext.Transform;
            if (target == null || !agent.isOnNavMesh)
            {
                return;
            }

            Vector3 away = transform.position - target.position;
            away.y = 0f;
            float minDistance = definition.Radius + PlayerContext.Radius;
            float distance = away.magnitude;
            if (distance >= minDistance || distance < 0.0001f)
            {
                return;
            }

            agent.Move(away / distance * (minDistance - distance));
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void TryAttack()
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackTimer = definition.AttackCooldown;

            var hits = attackResolver.ResolveHits(
                transform.position,
                transform.forward,
                definition.AttackRange,
                definition.Radius,
                gameObject,
                definition.Height * 0.5f
            );
            foreach (IDamageable hit in hits)
            {
                hit.ApplyDamage(new DamageInfo(definition.AttackDamage));
            }
        }

        // Personnage voxel généré en code (même pipeline CharacterModel que le joueur, voir
        // Assets/_Project/Scripts/CharacterModel), remplaçant l'ancienne capsule colorée
        // placeholder. GetInstanceID() donne un seed stable pour la durée de vie de l'ennemi
        // sans plomberie supplémentaire (deux ennemis du même EnemyDefinition varient donc
        // légèrement en couleur/proportions, comme deux joueurs avec des seeds différents).
        private void CreateVisual()
        {
            GameObject visual = CharacterModelBuilder.Build(
                definition.Height,
                definition.Archetype,
                seed: gameObject.GetInstanceID()
            );
            visual.name = "Visual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
        }
    }
}
