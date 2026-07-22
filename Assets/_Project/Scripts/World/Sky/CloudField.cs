using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CubeWorld.World
{
    /// <summary>
    /// Dérive des nuages autour de la cible de vue : wrap toroïdal sur un carré
    /// centré sur le joueur (densité constante quand on avance, pas de respawn
    /// visible contre le vent).
    /// </summary>
    internal sealed class CloudField
    {
        public struct CloudInstance
        {
            public Vector3 Position;
            public int VariantIndex;
            public float SpeedMultiplier;
            public float Scale;
        }

        private readonly CloudInstance[] clouds;

        public CloudField(CloudConfig config, Vector2 viewCenterXZ, ref Random rng)
        {
            float halfExtent = config.RecycleRadius;
            clouds = new CloudInstance[Mathf.Max(1, config.CloudCount)];

            for (int i = 0; i < clouds.Length; i++)
            {
                CloudInstance cloud = CreateCloud(config, ref rng);

                // Répartition uniforme dans le carré de wrap (pas un disque
                // biaisé vers le centre) pour couvrir tout le ciel.
                cloud.Position.x = viewCenterXZ.x + rng.NextFloat(-halfExtent, halfExtent);
                cloud.Position.z = viewCenterXZ.y + rng.NextFloat(-halfExtent, halfExtent);

                clouds[i] = cloud;
            }
        }

        public CloudInstance[] Clouds => clouds;

        public void Advance(float deltaTime, Vector2 windVelocity, Vector2 viewCenterXZ, CloudConfig config)
        {
            float halfExtent = config.RecycleRadius;
            float wrapSize = halfExtent * 2f;
            var windMove = new Vector3(windVelocity.x, 0f, windVelocity.y);

            for (int i = 0; i < clouds.Length; i++)
            {
                CloudInstance cloud = clouds[i];
                cloud.Position += windMove * (cloud.SpeedMultiplier * deltaTime);
                cloud.Position = WrapAroundView(cloud.Position, viewCenterXZ, halfExtent, wrapSize);
                clouds[i] = cloud;
            }
        }

        // Ramène le nuage dans [-half, half] relatif à la vue, sans changer
        // variante / échelle : le wrap est invisible si le rendu est culled
        // avant le bord (voir CloudSystem).
        private static Vector3 WrapAroundView(
            Vector3 position,
            Vector2 viewCenterXZ,
            float halfExtent,
            float wrapSize
        )
        {
            float x = position.x - viewCenterXZ.x;
            float z = position.z - viewCenterXZ.y;

            // while : couvre un gros saut de caméra (téléport debug) en une frame.
            while (x > halfExtent)
            {
                x -= wrapSize;
            }

            while (x < -halfExtent)
            {
                x += wrapSize;
            }

            while (z > halfExtent)
            {
                z -= wrapSize;
            }

            while (z < -halfExtent)
            {
                z += wrapSize;
            }

            return new Vector3(viewCenterXZ.x + x, position.y, viewCenterXZ.y + z);
        }

        private static CloudInstance CreateCloud(CloudConfig config, ref Random rng)
        {
            float altitude = rng.NextFloat(config.AltitudeMin, config.AltitudeMax);

            return new CloudInstance
            {
                Position = new Vector3(0f, altitude, 0f),
                VariantIndex = rng.NextInt(0, Mathf.Max(1, config.CloudVariantCount)),
                SpeedMultiplier = rng.NextFloat(0.85f, 1.15f),
                Scale = rng.NextFloat(0.75f, 1.35f),
            };
        }
    }
}
