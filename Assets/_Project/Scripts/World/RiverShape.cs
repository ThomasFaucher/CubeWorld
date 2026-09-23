using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Rivières et lacs : creusent localement la hauteur de surface sous le niveau de la mer
    /// — <see cref="TerrainShape.CreateVoxel"/> inonde déjà tout ce qui se trouve sous ce
    /// niveau, donc creuser suffit à faire apparaître l'eau, sans règle dédiée. Fonctions
    /// pures, appliquées après <see cref="TerrainShape.SampleHeight"/> (relief de base +
    /// biome) et avant <see cref="TerrainGenerationJob"/> ne remplit la colonne.
    /// </summary>
    internal static class RiverShape
    {
        // Bruit "ridge" à basse fréquence : proche de 1 sur une fine ligne serpentante là où
        // le Perlin change de signe. Seul, ce maillage de crêtes est DENSE partout sur la
        // carte (une ligne tous les ~80 voxels dans n'importe quelle direction) — c'est le
        // masque de présence ci-dessous qui le rend rare, en n'autorisant le carve que dans
        // une minorité de régions.
        private const float RiverNoiseScale = 0.006f;
        private const float RiverHalfWidth = 0.06f;
        private const int RiverCarveDepth = 5;

        // Masque de présence : bruit fractal à très basse fréquence, indépendant du bruit de
        // crête (décalage fixe additionnel) — seules les zones où il dépasse le seuil
        // peuvent avoir une rivière, le reste n'en a jamais, même si le maillage de crêtes
        // le "voudrait" localement. Ce seuil ET la bande basses-terres ci-dessous doivent
        // être vrais en même temps (ET logique) : les avoir chacun trop stricts les rendait
        // quasi jamais simultanés (rivières introuvables) — rester généreux sur les deux.
        private const float RiverPresenceNoiseScale = 0.0050f;
        private const int RiverPresenceOctaves = 3;
        private const float RiverPresenceThreshold = 0.52f;
        private const float RiverPresenceMargin = 0.14f;
        private static readonly float2 RiverPresenceOffset = new(777.1f, 333.7f);

        // Bassins (lacs) : bruit fractal basse fréquence indépendant du biome — de larges
        // taches occasionnelles, pas une grille régulière de lacs.
        private const float BasinNoiseScale = 0.0025f;
        private const int BasinOctaves = 3;
        private const float BasinThreshold = 0.62f;
        private const int BasinCarveDepth = 4;

        // Ni les rivières ni les lacs ne doivent trancher un relief élevé (mer/lac au sommet
        // d'une montagne n'a aucun sens) : le carve s'annule progressivement dès que la
        // surface est notablement au-dessus du niveau de la mer, quel que soit le bruit.
        // Bande volontairement resserrée : un carve partiel (voir CarveTowardSeaFloor) ne vise
        // le fond marin qu'une fois déjà proche du niveau de la mer — une bande large donnerait
        // des cratères secs (jamais inondés) sur du relief encore nettement au-dessus de l'eau.
        private const int LowlandBandAboveSeaLevel = 12;

        /// <summary>Hauteur de surface après creusement éventuel d'une rivière puis d'un bassin.</summary>
        public static int ApplyRivers(
            int worldX,
            int worldZ,
            int surfaceHeight,
            float2 riverSeedOffset,
            float2 basinSeedOffset,
            int seaLevel
        )
        {
            surfaceHeight = ApplyRiverChannel(
                worldX,
                worldZ,
                surfaceHeight,
                riverSeedOffset,
                seaLevel
            );
            surfaceHeight = ApplyBasin(worldX, worldZ, surfaceHeight, basinSeedOffset, seaLevel);
            return surfaceHeight;
        }

        private static int ApplyRiverChannel(
            int worldX,
            int worldZ,
            int surfaceHeight,
            float2 seedOffset,
            int seaLevel
        )
        {
            // Gates les moins chers d'abord (aucun bruit à fréquence "chenal" si la zone n'a
            // de toute façon pas de rivière ou est trop haute pour en avoir une).
            float lowland = LowlandFactor(surfaceHeight, seaLevel);
            if (lowland <= 0f)
            {
                return surfaceHeight;
            }

            float presence = RiverPresence(worldX, worldZ, seedOffset);
            if (presence <= 0f)
            {
                return surfaceHeight;
            }

            float2 point = (new float2(worldX, worldZ) + seedOffset) * RiverNoiseScale;
            float ridge = 1f - math.abs(noise.cnoise(point));
            float carve = math.smoothstep(1f - RiverHalfWidth, 1f, ridge) * presence * lowland;
            return CarveTowardSeaFloor(surfaceHeight, carve, lowland, seaLevel, RiverCarveDepth);
        }

        private static int ApplyBasin(
            int worldX,
            int worldZ,
            int surfaceHeight,
            float2 seedOffset,
            int seaLevel
        )
        {
            float lowland = LowlandFactor(surfaceHeight, seaLevel);
            if (lowland <= 0f)
            {
                return surfaceHeight;
            }

            float2 point = (new float2(worldX, worldZ) + seedOffset) * BasinNoiseScale;
            float basin = TerrainShape.Fbm(point, BasinOctaves);
            float carve = math.smoothstep(BasinThreshold, 1f, basin) * lowland;
            return CarveTowardSeaFloor(surfaceHeight, carve, lowland, seaLevel, BasinCarveDepth);
        }

        // Bruit à très basse fréquence, décorrélé du bruit de crête (décalage fixe en plus de
        // la seed du monde) : ne dépasse le seuil que dans une minorité de larges régions,
        // qui deviennent les seules zones où une rivière peut exister.
        private static float RiverPresence(int worldX, int worldZ, float2 seedOffset)
        {
            float2 point =
                (new float2(worldX, worldZ) + seedOffset + RiverPresenceOffset)
                * RiverPresenceNoiseScale;
            float value = TerrainShape.Fbm(point, RiverPresenceOctaves);
            return math.smoothstep(
                RiverPresenceThreshold - RiverPresenceMargin,
                RiverPresenceThreshold + RiverPresenceMargin,
                value
            );
        }

        // 1 au niveau de la mer, s'annule en douceur en montant — une rivière/un lac ne
        // doit jamais apparaître loin au-dessus du niveau de la mer.
        private static float LowlandFactor(int surfaceHeight, int seaLevel)
        {
            return 1f
                - math.smoothstep(seaLevel, seaLevel + LowlandBandAboveSeaLevel, surfaceHeight);
        }

        // Fondu (carve dans [0,1]) vers une profondeur creusée : jamais de mur vertical, la
        // berge descend progressivement au lieu de basculer d'un coup. La cible elle-même
        // dépend de "lowland" (proximité du niveau de la mer), pas seulement de "carve" : loin
        // de la mer, on ne vise qu'une légère ravine locale (surfaceHeight - carveDepth) — viser
        // directement le fond marin depuis un relief encore élevé ne ferait, pour la plupart des
        // valeurs de carve, que creuser un cratère sec qui ne redescend jamais sous l'eau.
        private static int CarveTowardSeaFloor(
            int surfaceHeight,
            float carve,
            float lowland,
            int seaLevel,
            int carveDepth
        )
        {
            if (carve <= 0f)
            {
                return surfaceHeight;
            }

            int localTarget = surfaceHeight - carveDepth;
            int seaTarget = seaLevel - 1 - carveDepth;
            int carvedHeight = (int)math.lerp(localTarget, seaTarget, lowland);
            return (int)math.lerp(surfaceHeight, carvedHeight, carve);
        }
    }
}
