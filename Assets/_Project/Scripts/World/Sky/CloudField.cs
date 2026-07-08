using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Dérive et recyclage des nuages, indépendant d'Unity (comme PlayerMotor) :
    /// calcule chaque frame la nouvelle position de chaque nuage, et le replace
    /// contre le vent quand il sort du rayon de vue.
    /// </summary>
    internal sealed class CloudField
    {
        public struct CloudInstance
        {
            public Vector3 Position;
            public int VariantIndex;
            public float SpeedMultiplier;
        }

        private readonly CloudInstance[] clouds;

        public CloudField(CloudConfig config, Vector2 viewCenterXZ, ref Random rng)
        {
            clouds = new CloudInstance[Mathf.Max(1, config.CloudCount)];

            for (int i = 0; i < clouds.Length; i++)
            {
                CloudInstance cloud = CreateCloud(config, ref rng);

                // Répartition initiale dans tout le disque de vue (pas
                // seulement au bord) : le ciel est déjà couvert au démarrage.
                float radius = rng.NextFloat(0f, config.RecycleRadius);
                float angle = rng.NextFloat(0f, math.PI * 2f);
                cloud.Position.x = viewCenterXZ.x + (math.cos(angle) * radius);
                cloud.Position.z = viewCenterXZ.y + (math.sin(angle) * radius);

                clouds[i] = cloud;
            }
        }

        public CloudInstance[] Clouds => clouds;

        public void Advance(
            float deltaTime,
            Vector2 windVelocity,
            Vector2 viewCenterXZ,
            CloudConfig config,
            ref Random rng
        )
        {
            float recycleRadiusSq = config.RecycleRadius * config.RecycleRadius;
            var windMove = new Vector3(windVelocity.x, 0f, windVelocity.y);

            for (int i = 0; i < clouds.Length; i++)
            {
                CloudInstance cloud = clouds[i];
                cloud.Position += windMove * (cloud.SpeedMultiplier * deltaTime);

                var positionXZ = new Vector2(cloud.Position.x, cloud.Position.z);
                if ((positionXZ - viewCenterXZ).sqrMagnitude > recycleRadiusSq)
                {
                    cloud = Recycle(cloud, config, viewCenterXZ, windVelocity, ref rng);
                }

                clouds[i] = cloud;
            }
        }

        // Replace le nuage contre le vent (bord du disque côté d'où vient le
        // vent), avec un décalage tangentiel aléatoire pour ne pas faire
        // réapparaître plusieurs nuages recyclés alignés au même endroit.
        private static CloudInstance Recycle(
            CloudInstance cloud,
            CloudConfig config,
            Vector2 viewCenterXZ,
            Vector2 windVelocity,
            ref Random rng
        )
        {
            Vector2 windDirection =
                windVelocity.sqrMagnitude > 0.0001f ? windVelocity.normalized : Vector2.right;
            Vector2 upwind = -windDirection * config.RecycleRadius;
            var tangent = new Vector2(-windDirection.y, windDirection.x);
            float tangentOffset = rng.NextFloat(-config.RecycleRadius, config.RecycleRadius);

            Vector2 newPositionXZ = viewCenterXZ + upwind + (tangent * tangentOffset);

            cloud.Position = new Vector3(newPositionXZ.x, cloud.Position.y, newPositionXZ.y);
            cloud.VariantIndex = rng.NextInt(0, Mathf.Max(1, config.CloudVariantCount));
            cloud.SpeedMultiplier = rng.NextFloat(0.9f, 1.1f);

            return cloud;
        }

        private static CloudInstance CreateCloud(CloudConfig config, ref Random rng)
        {
            float altitude = rng.NextFloat(config.AltitudeMin, config.AltitudeMax);

            return new CloudInstance
            {
                Position = new Vector3(0f, altitude, 0f),
                VariantIndex = rng.NextInt(0, Mathf.Max(1, config.CloudVariantCount)),
                SpeedMultiplier = rng.NextFloat(0.9f, 1.1f),
            };
        }
    }
}
