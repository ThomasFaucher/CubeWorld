namespace CubeWorld.Combat
{
    /// <summary>
    /// Décision de comportement d'un ennemi, indépendante d'Unity (comme
    /// PlayerMotor.ComputeMove) : ne connaît ni NavMeshAgent ni MonoBehaviour,
    /// facile à tester isolément.
    /// </summary>
    public sealed class EnemyAIBrain
    {
        public EnemyAction Decide(float distanceToTarget, float detectionRadius, float attackRange)
        {
            if (distanceToTarget > detectionRadius)
            {
                return EnemyAction.Idle;
            }

            return distanceToTarget <= attackRange ? EnemyAction.Attack : EnemyAction.Chase;
        }
    }
}
