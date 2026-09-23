using CubeWorld.Combat;
using NUnit.Framework;

namespace CubeWorld.Tests.EditMode
{
    public sealed class EnemyAIBrainTests
    {
        private EnemyAIBrain brain;

        [SetUp]
        public void SetUp()
        {
            brain = new EnemyAIBrain();
        }

        [Test]
        public void Decide_ReturnsIdle_WhenBeyondDetectionRadius()
        {
            EnemyAction action = brain.Decide(
                distanceToTarget: 11f,
                detectionRadius: 10f,
                attackRange: 1.5f
            );

            Assert.AreEqual(EnemyAction.Idle, action);
        }

        [Test]
        public void Decide_ReturnsChase_WhenWithinDetectionButOutOfAttackRange()
        {
            EnemyAction action = brain.Decide(
                distanceToTarget: 5f,
                detectionRadius: 10f,
                attackRange: 1.5f
            );

            Assert.AreEqual(EnemyAction.Chase, action);
        }

        [Test]
        public void Decide_ReturnsAttack_WhenWithinAttackRange()
        {
            EnemyAction action = brain.Decide(
                distanceToTarget: 1f,
                detectionRadius: 10f,
                attackRange: 1.5f
            );

            Assert.AreEqual(EnemyAction.Attack, action);
        }

        [Test]
        public void Decide_AtExactDetectionRadius_IsStillWithinRange()
        {
            // La comparaison est ">" (pas ">="), donc être exactement à la limite
            // compte encore comme détecté.
            EnemyAction action = brain.Decide(
                distanceToTarget: 10f,
                detectionRadius: 10f,
                attackRange: 1.5f
            );

            Assert.AreEqual(EnemyAction.Chase, action);
        }

        [Test]
        public void Decide_AtExactAttackRange_ReturnsAttack()
        {
            // La comparaison est "<=", donc être exactement à portée d'attaque suffit.
            EnemyAction action = brain.Decide(
                distanceToTarget: 1.5f,
                detectionRadius: 10f,
                attackRange: 1.5f
            );

            Assert.AreEqual(EnemyAction.Attack, action);
        }
    }
}
