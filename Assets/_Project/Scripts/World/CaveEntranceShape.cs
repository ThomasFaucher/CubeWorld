using Unity.Mathematics;

namespace CubeWorld.World
{
    /// <summary>
    /// Entrées de grottes rares (dolines / gouffres) : points d'intérêt ponctuels
    /// placés via une grille cellulaire à grande maille, qui percent la surface en
    /// puits vertical en entonnoir jusqu'à la première vraie poche de grotte
    /// (<see cref="CaveShape.IsCaveNoise"/>). Séparé de <see cref="CaveShape"/> qui,
    /// lui, refuse volontairement de trouer le paysage (SurfaceMargin).
    /// </summary>
    internal static class CaveEntranceShape
    {
        // Grille ~200 voxels + ~1 cellule sur 8 : points d'intérêt vraiment rares
        // (avant : 160 / 0.25 → trop de dolines croisées en explorant un peu).
        private const int EntranceCellSize = 200;
        private const float EntranceActivationChance = 0.12f;

        // Profil en entonnoir : large en bouche, qui se resserre en descendant.
        private const float EntranceMouthRadius = 3.5f;
        private const float EntranceThroatRadius = 1.5f;
        private const int EntranceFunnelDepth = 6;

        // Pas de gouffre sous l'eau ni sur une berge.
        private const int EntranceMinSeaMargin = 6;

        // Recherche de la première vraie poche de grotte sous la colonne ; si rien
        // n'est trouvé, profondeur de repli (petite grotte explorable en soi).
        private const int EntranceMinSearchDepth = 4;
        private const int EntranceMaxSearchDepth = 40;
        private const int EntranceFallbackDepth = 12;

        // Décalage fixe pour décorréler le hash d'entrée du bruit de terrain / rivières.
        private static readonly float2 EntranceHashOffset = new(911.3f, 271.9f);

        /// <summary>
        /// Vrai si la colonne (worldX, worldZ) tombe dans le rayon de bouche d'une
        /// entrée active. Remplit <paramref name="shaftBottomY"/> (fond du puits) et
        /// <paramref name="centerXZ"/> (centre jitté de l'entrée).
        /// </summary>
        public static bool TryGetShaft(
            int worldX,
            int worldZ,
            int surfaceHeight,
            float2 entranceSeedOffset,
            float2 caveSeedOffset,
            int seaLevel,
            out int shaftBottomY,
            out float2 centerXZ
        )
        {
            shaftBottomY = 0;
            centerXZ = default;

            if (surfaceHeight < seaLevel + EntranceMinSeaMargin)
            {
                return false;
            }

            // Cellule courante + 8 voisines : le centre jitté d'une cellule peut
            // déborder dans une cellule adjacente.
            int cellX = FloorDiv(worldX, EntranceCellSize);
            int cellZ = FloorDiv(worldZ, EntranceCellSize);

            float bestDistSq = float.MaxValue;
            float2 bestCenter = default;
            bool found = false;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!TrySampleCellEntrance(
                            cellX + dx,
                            cellZ + dz,
                            entranceSeedOffset,
                            out float2 candidateCenter
                        ))
                    {
                        continue;
                    }

                    float2 delta = new float2(worldX, worldZ) - candidateCenter;
                    float distSq = math.lengthsq(delta);
                    if (distSq > EntranceMouthRadius * EntranceMouthRadius)
                    {
                        continue;
                    }

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCenter = candidateCenter;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return false;
            }

            centerXZ = bestCenter;
            shaftBottomY = FindShaftBottom(worldX, worldZ, surfaceHeight, caveSeedOffset);
            return true;
        }

        /// <summary>
        /// Vrai si ce voxel tombe dans le puits d'entrée (distance XZ ≤ rayon à cette
        /// profondeur, et au-dessus du fond du puits).
        /// </summary>
        public static bool IsInsideShaft(
            int worldX,
            int worldY,
            int worldZ,
            int surfaceHeight,
            int shaftBottomY,
            float2 centerXZ
        )
        {
            if (worldY > surfaceHeight || worldY < shaftBottomY)
            {
                return false;
            }

            float depth = surfaceHeight - worldY;
            float radius = RadiusAt(depth);
            float2 delta = new float2(worldX, worldZ) - centerXZ;
            return math.lengthsq(delta) <= radius * radius;
        }

        private static float RadiusAt(float depthBelowSurface)
        {
            if (depthBelowSurface <= 0f)
            {
                return EntranceMouthRadius;
            }

            if (depthBelowSurface >= EntranceFunnelDepth)
            {
                return EntranceThroatRadius;
            }

            float t = depthBelowSurface / EntranceFunnelDepth;
            return math.lerp(EntranceMouthRadius, EntranceThroatRadius, t);
        }

        // Hash par cellule via Perlin aux coordonnées entières (déterministe, Burst-safe)
        // : activation + jitter du centre à l'intérieur de la cellule.
        private static bool TrySampleCellEntrance(
            int cellX,
            int cellZ,
            float2 entranceSeedOffset,
            out float2 centerXZ
        )
        {
            float2 hashPoint =
                new float2(cellX, cellZ) + entranceSeedOffset * 0.001f + EntranceHashOffset;
            float activation = noise.cnoise(hashPoint) * 0.5f + 0.5f;
            if (activation > EntranceActivationChance)
            {
                centerXZ = default;
                return false;
            }

            // Jitter décorrélé (axes séparés) pour éviter un alignement sur la grille.
            float jitterX = noise.cnoise(hashPoint + new float2(17.1f, 0f)) * 0.5f + 0.5f;
            float jitterZ = noise.cnoise(hashPoint + new float2(0f, 31.7f)) * 0.5f + 0.5f;

            // Garde une marge MouthRadius depuis le bord pour limiter le débordement
            // excessif tout en laissant le test 3×3 voisinage gérer le reste.
            float margin = EntranceMouthRadius;
            float usable = EntranceCellSize - 2f * margin;
            if (usable < 1f)
            {
                usable = EntranceCellSize;
                margin = 0f;
            }

            centerXZ = new float2(
                cellX * EntranceCellSize + margin + jitterX * usable,
                cellZ * EntranceCellSize + margin + jitterZ * usable
            );
            return true;
        }

        // Première vraie poche de grotte sous la colonne (sans SurfaceMargin) ; sinon
        // profondeur de repli bornée.
        private static int FindShaftBottom(
            int worldX,
            int worldZ,
            int surfaceHeight,
            float2 caveSeedOffset
        )
        {
            int minY = surfaceHeight - EntranceMaxSearchDepth;
            int startY = surfaceHeight - EntranceMinSearchDepth;

            for (int y = startY; y >= minY; y--)
            {
                if (CaveShape.IsCaveNoise(worldX, y, worldZ, caveSeedOffset))
                {
                    return y;
                }
            }

            // Repli borné : ne jamais descendre sous le plancher de bedrock des grottes.
            int fallback = surfaceHeight - EntranceFallbackDepth;
            return math.max(fallback, 3);
        }

        // Division entière vers -∞ (C# `/` tronque vers 0, ce qui casse les cellules
        // aux coordonnées négatives).
        private static int FloorDiv(int value, int divisor)
        {
            int q = value / divisor;
            int r = value % divisor;
            if (r != 0 && ((value < 0) != (divisor < 0)))
            {
                q--;
            }

            return q;
        }
    }
}
