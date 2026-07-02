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

            CreatePlaceholderVisual();
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            attackTimer -= Time.deltaTime;

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
                    agent.SetDestination(target.position);
                    break;

                case EnemyAction.Attack:
                    agent.ResetPath();
                    FaceTarget(target.position);
                    TryAttack();
                    break;
            }
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

        // Représentation visuelle temporaire (une capsule colorée), même
        // approche que PlayerController.CreatePlaceholderVisual.
        private void CreatePlaceholderVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlaceholderVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, definition.Height * 0.5f, 0f);
            visual.transform.localScale = new Vector3(
                definition.Radius * 2f,
                definition.Height * 0.5f,
                definition.Radius * 2f
            );
            Destroy(visual.GetComponent<Collider>());

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                visual.GetComponent<Renderer>().sharedMaterial = new Material(shader)
                {
                    color = definition.BodyColor,
                };
            }
        }
    }
}
