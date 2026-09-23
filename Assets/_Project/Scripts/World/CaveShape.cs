using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Grottes et veines de minerai : bruit 3D appliqué voxel par voxel après le
    /// remplissage de colonne de <see cref="TerrainShape.CreateVoxel"/> — jamais dans les
    /// <see cref="SurfaceMargin"/> voxels sous la surface, pour ne jamais trouer le
    /// paysage visible depuis le dessus. Les veines de minerai ne remplacent que de la
    /// Pierre (jamais la Terre/le Sable, trop proches de la surface pour avoir un sens),
    /// avec une rareté croissante avec la profondeur — voir CubeWorld.Player.PlayerMining
    /// (assemblée CubeWorld.Player, non référençable ici) pour les rendre minables.
    /// </summary>
    internal static class CaveShape
    {
        // Seuil haut : à 0.62 le FBM 3D (centré ~0.5) perforait ~25%+ de la pierre en
        // cavernes connectées — une doline en surface te faisait tomber jusqu'au vide sous
        // le monde. 0.75 → poches nettement plus rares et séparées.
        private const float CaveNoiseScale = 0.045f;
        private const int CaveOctaves = 2;
        private const float CaveThreshold = 0.75f;
        private const int SurfaceMargin = 4;

        // Plancher solide : jamais de grotte sous ce Y — empêche de traverser le monde
        // jusqu'aux chunks vides en dessous (chute infinie).
        private const int BedrockFloor = 2;

        private const float OreNoiseScale = 0.09f;
        private const int OreOctaves = 2;
        private const float CopperThreshold = 0.74f;
        private const float IronThreshold = 0.80f;
        private const float GoldThreshold = 0.865f;
        private const int IronMinDepth = 12;
        private const int GoldMinDepth = 28;

        // Décalage arbitraire (mais fixe) pour que le bruit de minerai ne corrèle pas avec
        // celui des grottes malgré la même SeedOffset de départ.
        private static readonly float3 OreOffset = new(500f, 500f, 500f);

        /// <summary>Vrai si ce voxel doit être creusé en air (grotte).</summary>
        public static bool IsCave(int worldX, int worldY, int worldZ, int surfaceHeight, float2 seedOffset)
        {
            if (worldY <= BedrockFloor || worldY > surfaceHeight - SurfaceMargin)
            {
                return false;
            }

            return IsCaveNoise(worldX, worldY, worldZ, seedOffset);
        }

        /// <summary>
        /// Bruit 3D de grotte seul, sans la garde <see cref="SurfaceMargin"/> — utilisé
        /// par <see cref="CaveEntranceShape"/> pour chercher une poche sous une entrée
        /// (doline) qui, elle, a le droit de percer la surface. Respecte quand même le
        /// plancher <see cref="BedrockFloor"/> (pas de chute dans le vide).
        /// </summary>
        public static bool IsCaveNoise(int worldX, int worldY, int worldZ, float2 seedOffset)
        {
            if (worldY <= BedrockFloor)
            {
                return false;
            }

            float3 point =
                (new float3(worldX, worldY, worldZ) + new float3(seedOffset.x, 0f, seedOffset.y))
                * CaveNoiseScale;
            return Fbm3D(point, CaveOctaves) > CaveThreshold;
        }

        /// <summary>Minerai qui remplace ce voxel de Pierre à cette profondeur, ou Stone si aucun.</summary>
        public static VoxelType ApplyOreVein(int worldX, int worldY, int worldZ, int surfaceHeight, float2 seedOffset)
        {
            int depth = surfaceHeight - worldY;
            float3 point = (new float3(worldX, worldY, worldZ) + new float3(seedOffset.x, 0f, seedOffset.y) + OreOffset) * OreNoiseScale;
            float vein = Fbm3D(point, OreOctaves);

            if (depth >= GoldMinDepth && vein > GoldThreshold)
            {
                return VoxelType.OreGold;
            }

            if (depth >= IronMinDepth && vein > IronThreshold)
            {
                return VoxelType.OreIron;
            }

            if (vein > CopperThreshold)
            {
                return VoxelType.OreCopper;
            }

            return VoxelType.Stone;
        }

        // Même formule que TerrainShape.Fbm, en 3D — pas de partage direct possible, Fbm
        // travaillant sur float2 (utilisé par du code qui, lui, doit rester en 2D).
        private static float Fbm3D(float3 point, int octaves)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float totalAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * ((noise.cnoise(point * frequency) * 0.5f) + 0.5f);
                totalAmplitude += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return sum / totalAmplitude;
        }
    }
}
