using CubeWorld.Player;
using NUnit.Framework;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    public sealed class PlayerMotorTests
    {
        private const float Dt = 0.02f;
        private const float Gravity = -20f;

        private PlayerMotor motor;

        [SetUp]
        public void SetUp()
        {
            motor = new PlayerMotor(
                walkSpeed: 6f,
                sprintMultiplier: 1.8f,
                jumpHeight: 1.5f,
                gravity: Gravity
            );
        }

        private void Step(bool grounded, bool jump = false, float dt = Dt)
        {
            motor.ComputeMove(
                Vector2.zero,
                sprint: false,
                jumpPressed: jump,
                isGrounded: grounded,
                cameraYaw: 0f,
                deltaTime: dt
            );
        }

        // Avance de `duration` secondes en l'air, sans appuyer sur Saut.
        private void FallFor(float duration)
        {
            for (float t = 0f; t < duration - 0.0001f; t += Dt)
            {
                Step(grounded: false);
            }
        }

        [Test]
        public void Jump_FromGround_GivesUpwardVelocity()
        {
            Step(grounded: true, jump: true);

            Assert.Greater(motor.VerticalVelocity, 0f);
        }

        [Test]
        public void Jump_ShortlyAfterLeavingGround_IsAccepted()
        {
            Step(grounded: true);
            FallFor(0.08f);

            Step(grounded: false, jump: true);

            Assert.Greater(motor.VerticalVelocity, 0f);
        }

        [Test]
        public void Jump_LongAfterLeavingGround_IsRefused()
        {
            Step(grounded: true);
            FallFor(0.2f);

            Step(grounded: false, jump: true);

            Assert.Less(motor.VerticalVelocity, 0f);
        }

        [Test]
        public void Jump_PressedJustBeforeLanding_TriggersOnLanding()
        {
            Step(grounded: true);
            FallFor(0.4f);
            Step(grounded: false, jump: true);
            FallFor(0.06f);

            Step(grounded: true);

            Assert.Greater(motor.VerticalVelocity, 0f);
        }

        [Test]
        public void Jump_CannotChainSecondJumpInAir()
        {
            Step(grounded: true, jump: true);
            Step(grounded: false);
            float velocityBefore = motor.VerticalVelocity;

            Step(grounded: false, jump: true);

            Assert.Less(motor.VerticalVelocity, velocityBefore);
        }

        [Test]
        public void HittingCeiling_WhileRising_StopsUpwardVelocity()
        {
            Step(grounded: true, jump: true);

            motor.NotifyCollisions(CollisionFlags.Above);

            Assert.AreEqual(0f, motor.VerticalVelocity);
        }

        [Test]
        public void HittingCeiling_WhileFalling_KeepsVelocity()
        {
            Step(grounded: true);
            FallFor(0.5f);
            float velocityBefore = motor.VerticalVelocity;

            motor.NotifyCollisions(CollisionFlags.Above);

            Assert.AreEqual(velocityBefore, motor.VerticalVelocity);
        }

        [Test]
        public void LongFall_IsClampedToMaxFallSpeed()
        {
            Step(grounded: true);
            FallFor(10f);

            Assert.AreEqual(-PlayerMotor.MaxFallSpeed, motor.VerticalVelocity, 0.0001f);
        }
    }
}
