using System.Collections.Generic;
using CubeWorld.Combat;
using CubeWorld.Core;
using NUnit.Framework;
using UnityEngine;

namespace CubeWorld.Tests.EditMode
{
    public sealed class MeleeAttackResolverTests
    {
        private readonly List<GameObject> spawned = new();
        private MeleeAttackResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new MeleeAttackResolver();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void ResolveHits_FindsTarget_InFrontOfAttacker()
        {
            GameObject attacker = CreateAttacker(Vector3.zero);
            TestDamageable target = CreateTarget(new Vector3(0f, 0.5f, 1.5f));
            SyncPhysics();

            IReadOnlyList<IDamageable> hits = resolver.ResolveHits(
                attacker.transform.position,
                Vector3.forward,
                range: 3f,
                radius: 0.75f,
                attacker: attacker,
                originHeight: 1f
            );

            Assert.AreEqual(1, hits.Count);
            Assert.AreSame(target, hits[0]);
        }

        [Test]
        public void ResolveHits_ExcludesCollidersOnTheAttackerItself()
        {
            GameObject attacker = CreateAttacker(Vector3.zero);
            // Simule le propre collider de hitbox de l'attaquant (ex. CapsuleCollider posé
            // par EnemyAI.Initialize sur le même GameObject) : ne doit jamais se toucher.
            attacker.AddComponent<SphereCollider>().radius = 0.5f;
            SyncPhysics();

            IReadOnlyList<IDamageable> hits = resolver.ResolveHits(
                attacker.transform.position,
                Vector3.forward,
                range: 3f,
                radius: 0.75f,
                attacker: attacker,
                originHeight: 1f
            );

            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void ResolveHits_Deduplicates_WhenTargetHasMultipleColliders()
        {
            GameObject attacker = CreateAttacker(Vector3.zero);
            GameObject targetRoot = new("Target");
            spawned.Add(targetRoot);
            targetRoot.transform.position = new Vector3(0f, 0.5f, 1.5f);
            TestDamageable target = targetRoot.AddComponent<TestDamageable>();
            targetRoot.AddComponent<SphereCollider>().radius = 0.4f;

            var secondCollider = new GameObject("SecondCollider");
            spawned.Add(secondCollider);
            secondCollider.transform.SetParent(targetRoot.transform, false);
            secondCollider.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            secondCollider.AddComponent<SphereCollider>().radius = 0.4f;
            SyncPhysics();

            IReadOnlyList<IDamageable> hits = resolver.ResolveHits(
                attacker.transform.position,
                Vector3.forward,
                range: 3f,
                radius: 0.75f,
                attacker: attacker,
                originHeight: 1f
            );

            Assert.AreEqual(1, hits.Count, "Deux colliders du même IDamageable ne doivent compter qu'une fois.");
            Assert.AreSame(target, hits[0]);
        }

        [Test]
        public void ResolveHits_IgnoresTarget_OutOfRange()
        {
            GameObject attacker = CreateAttacker(Vector3.zero);
            CreateTarget(new Vector3(0f, 0.5f, 10f));
            SyncPhysics();

            IReadOnlyList<IDamageable> hits = resolver.ResolveHits(
                attacker.transform.position,
                Vector3.forward,
                range: 3f,
                radius: 0.75f,
                attacker: attacker,
                originHeight: 1f
            );

            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void ResolveHits_ReturnsEmpty_WhenForwardIsPurelyVertical()
        {
            GameObject attacker = CreateAttacker(Vector3.zero);
            CreateTarget(new Vector3(0f, 0.5f, 1.5f));
            SyncPhysics();

            // flatForward = Vector3.ProjectOnPlane(Vector3.up, Vector3.up) == quasi-zéro.
            IReadOnlyList<IDamageable> hits = resolver.ResolveHits(
                attacker.transform.position,
                Vector3.up,
                range: 3f,
                radius: 0.75f,
                attacker: attacker,
                originHeight: 1f
            );

            Assert.AreEqual(0, hits.Count);
        }

        private GameObject CreateAttacker(Vector3 position)
        {
            var attacker = new GameObject("Attacker");
            spawned.Add(attacker);
            attacker.transform.position = position;
            return attacker;
        }

        private TestDamageable CreateTarget(Vector3 position)
        {
            var target = new GameObject("Target");
            spawned.Add(target);
            target.transform.position = position;
            target.AddComponent<SphereCollider>().radius = 0.4f;
            return target.AddComponent<TestDamageable>();
        }

        // Force la mise à jour de la broadphase physique : en édit-mode, rien ne simule les
        // FixedUpdate qui synchronisent normalement les Transforms vers les colliders.
        private static void SyncPhysics() => Physics.SyncTransforms();

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public bool IsDead => false;

            public void ApplyDamage(DamageInfo info) { }
        }
    }
}
