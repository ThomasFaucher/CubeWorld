using CubeWorld.World;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWorld.Player
{
    /// <summary>
    /// Poussière de pas cartoon : émission par distance parcourue au sol, teinte
    /// et densité selon le biome (affinée par le voxel sous les pieds), petit
    /// burst à l'atterrissage.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootstepDust : MonoBehaviour
    {
        [SerializeField] private float _stepDistance = 0.45f;
        [SerializeField] private int _particlesPerStep = 4;
        [SerializeField] private int _landingBurst = 8;

        private CharacterController controller;
        private WorldBootstrap worldBootstrap;
        private ParticleSystem dust;
        private ParticleSystem.EmitParams emitParams;

        private Vector3 lastPosition;
        private float distanceAccumulator;
        private bool wasGrounded = true;

        /// <summary>Appelé depuis PlayerBootstrap après création du joueur.</summary>
        public void Bind(WorldBootstrap bootstrap)
        {
            worldBootstrap = bootstrap;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastPosition = transform.position;
            dust = CreateDustSystem();
            emitParams = new ParticleSystem.EmitParams();
        }

        private void Update()
        {
            bool grounded = controller.isGrounded;
            Vector3 position = transform.position;

            if (grounded && wasGrounded)
            {
                Vector3 delta = position - lastPosition;
                delta.y = 0f;
                float moved = delta.magnitude;

                if (moved > 0.001f)
                {
                    distanceAccumulator += moved;
                    while (distanceAccumulator >= _stepDistance)
                    {
                        distanceAccumulator -= _stepDistance;
                        EmitStep(_particlesPerStep);
                    }
                }
            }
            else if (grounded && !wasGrounded)
            {
                distanceAccumulator = 0f;
                EmitStep(_landingBurst);
            }
            else if (!grounded)
            {
                distanceAccumulator = 0f;
            }

            wasGrounded = grounded;
            lastPosition = position;
        }

        private void EmitStep(int count)
        {
            if (dust == null || count <= 0)
            {
                return;
            }

            FootstepStyle style = SampleFootstepStyle();
            int emitCount = Mathf.Max(1, Mathf.RoundToInt(count * style.CountScale));

            Vector3 feet = transform.position;
            feet.y = transform.position.y - (controller.height * 0.5f) + controller.center.y + 0.05f;

            emitParams.position = feet;
            emitParams.startColor = (Color)style.Tint;
            emitParams.startSize = UnityEngine.Random.Range(style.SizeMin, style.SizeMax);

            dust.Emit(emitParams, emitCount);
        }

        private FootstepStyle SampleFootstepStyle()
        {
            VoxelWorld world = worldBootstrap != null ? worldBootstrap.World : null;
            if (world == null)
            {
                return FootstepStyle.Default;
            }

            int worldX = (int)math.floor(transform.position.x);
            int worldZ = (int)math.floor(transform.position.z);
            BiomeType biome = world.GetBiome(worldX, worldZ);

            int3 underFeet = (int3)math.floor(new float3(
                transform.position.x,
                transform.position.y - 0.15f,
                transform.position.z));

            Voxel voxel = world.GetVoxel(underFeet);
            if (voxel.IsAir)
            {
                underFeet.y -= 1;
                voxel = world.GetVoxel(underFeet);
            }

            return StyleForBiome(biome, voxel.Type);
        }

        private static FootstepStyle StyleForBiome(BiomeType biome, VoxelType ground)
        {
            // Biome d'abord ; le voxel affine (ex. pierre en forêt, neige en altitude).
            return biome switch
            {
                BiomeType.Plains => ground switch
                {
                    VoxelType.Dirt => new FootstepStyle(new Color32(155, 110, 60, 210), 0.05f, 0.11f, 1f),
                    VoxelType.Stone => new FootstepStyle(new Color32(155, 150, 140, 190), 0.03f, 0.07f, 0.75f),
                    _ => new FootstepStyle(new Color32(120, 175, 70, 210), 0.04f, 0.1f, 1f), // herbe chaude
                },
                BiomeType.Forest => ground switch
                {
                    VoxelType.Dirt => new FootstepStyle(new Color32(95, 70, 45, 220), 0.05f, 0.12f, 1.1f),
                    VoxelType.Stone => new FootstepStyle(new Color32(130, 140, 135, 190), 0.03f, 0.07f, 0.7f),
                    _ => new FootstepStyle(new Color32(55, 120, 50, 220), 0.045f, 0.11f, 1.15f), // feuilles / humus
                },
                BiomeType.Desert => ground switch
                {
                    VoxelType.Stone => new FootstepStyle(new Color32(180, 150, 110, 200), 0.04f, 0.09f, 1f),
                    _ => new FootstepStyle(new Color32(235, 195, 95, 230), 0.06f, 0.14f, 1.4f), // sable abondant
                },
                BiomeType.Snow => ground switch
                {
                    VoxelType.Ice => new FootstepStyle(new Color32(190, 220, 240, 180), 0.03f, 0.07f, 0.65f),
                    VoxelType.Stone => new FootstepStyle(new Color32(170, 180, 195, 190), 0.03f, 0.07f, 0.7f),
                    _ => new FootstepStyle(new Color32(245, 250, 255, 230), 0.05f, 0.12f, 1.25f), // neige
                },
                BiomeType.Swamp => ground switch
                {
                    VoxelType.Dirt => new FootstepStyle(new Color32(70, 85, 55, 230), 0.05f, 0.13f, 1.2f),
                    VoxelType.Stone => new FootstepStyle(new Color32(120, 130, 110, 190), 0.03f, 0.07f, 0.7f),
                    _ => new FootstepStyle(new Color32(60, 110, 55, 220), 0.05f, 0.12f, 1.15f), // herbe humide
                },
                _ => FootstepStyle.Default,
            };
        }

        private readonly struct FootstepStyle
        {
            public static FootstepStyle Default { get; } =
                new(new Color32(180, 160, 120, 200), 0.04f, 0.1f, 1f);

            public readonly Color32 Tint;
            public readonly float SizeMin;
            public readonly float SizeMax;
            public readonly float CountScale;

            public FootstepStyle(Color32 tint, float sizeMin, float sizeMax, float countScale)
            {
                Tint = tint;
                SizeMin = sizeMin;
                SizeMax = sizeMax;
                CountScale = countScale;
            }
        }

        private ParticleSystem CreateDustSystem()
        {
            var go = new GameObject("FootstepDust");
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new Color(0.75f, 0.65f, 0.45f, 0.75f);
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;
            shape.radiusThickness = 1f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader != null)
            {
                var material = new Material(shader);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", Color.white);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", Color.white);
                }

                renderer.sharedMaterial = material;
            }

            return ps;
        }
    }
}
