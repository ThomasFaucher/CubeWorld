using CubeWorld.Player.VoxelModels.Generation;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>
    /// Épéiste chibi généré procéduralement à partir d'un seed : palette, gabarit
    /// (taille de tête, largeur torse/bras, cape, pauldron, coiffure) et profil de
    /// tête varient d'un seed à l'autre, en gardant la silhouette reconnaissable.
    /// </summary>
    internal static class SwordsmanVoxelModel
    {
        private const int BaseHeightVoxels = 36;
        private const int BaseHeadSize = 20;

        // La tête est bien plus LARGE que HAUTE (silhouette ronde/ovale façon chibi) : le nombre
        // de rangées (hauteur) est un ratio de headSize (largeur), pas headSize lui-même —
        // sinon la tête serait un cube plein et paraîtrait trop haute/allongée verticalement.
        private const float HeadHeightRatio = 0.72f;
        private const int BaseHeadRows = 14; // ≈ Round(BaseHeadSize * HeadHeightRatio)

        // Layout "trapu" : jambes/torse courts pour laisser la tête (énorme, cf. BaseHeadSize)
        // dominer la silhouette façon référence chibi, au lieu de proportions plus réalistes.
        private const int LegBootHeight = 4;
        private const int LegCalfHeight = 2;
        private const int LegThighHeight = 2;
        private const int LegHeight = 1 + LegBootHeight + LegCalfHeight + LegThighHeight; // 9

        private const int TorsoBeltHeight = 1;
        private const int TorsoWaistHeight = 3;
        private const int TorsoChestHeight = 3;
        private const int TorsoHeight = TorsoBeltHeight + TorsoWaistHeight + TorsoChestHeight; // 7

        // Le cou "mord" sur la dernière rangée du torse (col embarqué dans l'encolure) et
        // n'ajoute qu'une seule rangée réellement nouvelle avant le début de la tête.
        private const int NeckExtraHeight = 1;
        private const int HeadBaseY = LegHeight + TorsoHeight + NeckExtraHeight; // 17

        public static Mesh Build(
            float targetHeight,
            int seed = 0,
            CharacterExpression expression = CharacterExpression.Neutral
        )
        {
            var rng = new CharacterRng(seed);
            SwordsmanPalette palette = SwordsmanPalette.Generate(rng);
            SwordsmanBodyParams body = SwordsmanBodyParams.Generate(rng);
            int headRows = Mathf.Max(8, Mathf.RoundToInt(body.HeadSize * HeadHeightRatio));
            HeadProfile profile = HeadProfile.Generate(body.HeadSize, headRows, rng);

            // La hauteur totale suit légèrement la taille de tête (via son nombre de rangées
            // réel, headRows, pas sa largeur) pour que la proportion "grosse tête chibi" reste
            // stable d'un seed à l'autre.
            int heightVoxels = BaseHeightVoxels + (headRows - BaseHeadRows);
            float unit = targetHeight / heightVoxels;
            var mesh = new PlayerVoxelMeshData();
            int seedCounter = 0;

            AddLeg(mesh, 1, palette, unit, ref seedCounter);
            AddLeg(mesh, -6, palette, unit, ref seedCounter);

            // Torse/ceinture : largeur pilotée par body.TorsoWidthScale, recentrée
            // pour que le personnage reste symétrique quel que soit le seed.
            int torsoWidth = ScaleLen(14, body.TorsoWidthScale);
            int torsoX0 = -7 - ((torsoWidth - 14) / 2);
            int halfTorso = torsoWidth / 2;

            // Rangées clés du torse, dérivées des constantes de layout : tout le reste du
            // torse/cou/bras s'accroche à ces variables pour rester cohérent si les hauteurs
            // de layout changent.
            int beltRow = LegHeight;
            int waistRow = beltRow + TorsoBeltHeight;
            int chestRow = waistRow + TorsoWaistHeight;
            int torsoTopRow = chestRow + TorsoChestHeight - 1;

            Add(mesh, new Vector3Int(torsoX0, beltRow, -3), torsoWidth, TorsoBeltHeight, 7, PlayerVoxelShape.Box, palette.Belt, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-1, beltRow, 4), 2, 1, 1, PlayerVoxelShape.Box, palette.Gem, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-3, beltRow, 4), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(2, beltRow, 4), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(7, beltRow - 3, -1), 2, 3, 3, PlayerVoxelShape.Box, palette.Pouch, unit, ref seedCounter);
            Add(mesh, new Vector3Int(7, beltRow - 2, 2), 2, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);

            // Torse : tunique en 2 paliers (taille resserrée + buste large, comme la tête est un
            // empilement de rayons variables) au lieu d'un seul pavé uniforme, plus plastron
            // proéminent + arête centrale + rivets.
            int waistWidth = Mathf.Max(8, Mathf.RoundToInt(torsoWidth * 0.84f));
            int waistX0 = -7 - ((waistWidth - 14) / 2);
            Add(mesh, new Vector3Int(waistX0, waistRow, -3), waistWidth, TorsoWaistHeight, 7, PlayerVoxelShape.Box, palette.Tunic, unit, ref seedCounter);
            Add(mesh, new Vector3Int(torsoX0, chestRow, -3), torsoWidth, TorsoChestHeight, 7, PlayerVoxelShape.Box, palette.Tunic, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-4, chestRow - 2, 4), 8, 5, 1, PlayerVoxelShape.Box, palette.Armor, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-1, chestRow - 1, 5), 2, 3, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-3, chestRow, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.Gem, unit, ref seedCounter);
            Add(mesh, new Vector3Int(2, chestRow, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.Gem, unit, ref seedCounter);
            // Coutures/rivets du plastron : petits accents qui cassent les grands aplats.
            Add(mesh, new Vector3Int(-4, chestRow - 1, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(3, chestRow - 1, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-4, chestRow + 2, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);
            Add(mesh, new Vector3Int(3, chestRow + 2, 5), 1, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seedCounter);

            // Cou : deux Column qui relient réellement la largeur du torse à celle de la
            // mâchoire (au lieu d'un plot 2x1x2 qui ne raccordait ni l'une ni l'autre) — un
            // "collier" large encastré dans l'encolure, puis un fût qui se resserre pile à la
            // largeur de la mâchoire pour fusionner sans marche avec la tête.
            int jawWidth = profile.RowWidth[0];
            int neckCollarWidth = Mathf.Min(torsoWidth - 2, jawWidth + 4);
            Color32 neckShadow = Darken(palette.Skin, 0.82f);
            Add(mesh, new Vector3Int(-(neckCollarWidth / 2), torsoTopRow, -(neckCollarWidth / 2)), neckCollarWidth, 1, neckCollarWidth, PlayerVoxelShape.Column, neckShadow, unit, ref seedCounter);
            Add(mesh, new Vector3Int(-(jawWidth / 2), torsoTopRow + 1, -(jawWidth / 2)), jawWidth, 1, jawWidth, PlayerVoxelShape.Column, palette.Skin, unit, ref seedCounter);

            // Bras attachés aux bords réels du torse (utile si torsoWidth varie) et alignés sur
            // torsoTopRow pour que le pauldron rejoigne exactement la base de la tête.
            int armWidth = ScaleLen(3, body.ArmThicknessScale);
            AddArm(mesh, halfTorso, armWidth, torsoTopRow, palette, body.Pauldron, unit, ref seedCounter);
            AddArm(mesh, -halfTorso - armWidth, armWidth, torsoTopRow, palette, body.Pauldron, unit, ref seedCounter);

            if (body.HasCape)
            {
                // Cape dans le dos : deux pans superposés + pointe effilée + fermoir + emblème,
                // tous accrochés aux rangées réelles du torse.
                int capeUpperY = chestRow - 2;
                int capeUpperHeight = torsoTopRow - capeUpperY + 1;
                int capeMidY = beltRow - 3;
                int capeMidHeight = capeUpperY - capeMidY;
                int capeTipY = Mathf.Max(0, capeMidY - 5);

                Add(mesh, new Vector3Int(-5, capeUpperY, -4), 10, capeUpperHeight, 1, PlayerVoxelShape.Box, palette.Cape, unit, ref seedCounter);
                Add(mesh, new Vector3Int(-6, capeMidY, -4), 12, capeMidHeight, 1, PlayerVoxelShape.Box, palette.CapeDark, unit, ref seedCounter);
                Add(mesh, new Vector3Int(-4, capeTipY, -4), 8, 5, 1, PlayerVoxelShape.ConeDown, palette.CapeDark, unit, ref seedCounter);
                Add(mesh, new Vector3Int(-1, torsoTopRow, -4), 2, 1, 1, PlayerVoxelShape.Box, palette.CapeTrim, unit, ref seedCounter);
                Add(mesh, new Vector3Int(-2, chestRow, -5), 4, 1, 1, PlayerVoxelShape.Box, palette.CapeTrim, unit, ref seedCounter);
                Add(mesh, new Vector3Int(-1, chestRow - 1, -5), 2, 3, 1, PlayerVoxelShape.Box, palette.CapeTrim, unit, ref seedCounter);
            }

            var headOrigin = new Vector3Int(-(body.HeadSize / 2), HeadBaseY, -(body.HeadSize / 2));
            AddHead(mesh, headOrigin, profile, palette, expression, unit, ref seedCounter);
            AddHair(mesh, headOrigin, profile, body.Hair, palette.Hair, unit, ref seedCounter);

            return mesh.ToMesh("PlayerVoxelModel_Swordsman");
        }

        private static void AddLeg(PlayerVoxelMeshData mesh, int x0, SwordsmanPalette palette, float unit, ref int seed)
        {
            // Semelle large, bottine, sangle proéminente et boucle. Jambe courte et trapue
            // (cf. LegHeight) pour laisser la tête dominer la silhouette.
            int calfRow = 1 + LegBootHeight;
            int thighRow = calfRow + LegCalfHeight;

            Add(mesh, new Vector3Int(x0 - 1, 0, -3), 7, 1, 7, PlayerVoxelShape.Box, palette.BootSole, unit, ref seed);
            Add(mesh, new Vector3Int(x0, 1, -2), 5, LegBootHeight, 5, PlayerVoxelShape.Box, palette.Boot, unit, ref seed);
            Add(mesh, new Vector3Int(x0, LegBootHeight, 3), 5, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seed);
            Add(mesh, new Vector3Int(x0 + 2, LegBootHeight, 4), 1, 1, 1, PlayerVoxelShape.Box, palette.Gem, unit, ref seed);

            // Jambière : mollet légèrement renflé (plus large) et cuisse plus étroite/sombre,
            // pour casser le tube uniforme, + grève proéminente + accent au genou.
            Color32 pantsShade = Darken(palette.Pants, 0.85f);
            Add(mesh, new Vector3Int(x0 - 1, calfRow, -3), 7, LegCalfHeight, 7, PlayerVoxelShape.Column, palette.Pants, unit, ref seed);
            Add(mesh, new Vector3Int(x0, thighRow, -2), 5, LegThighHeight, 5, PlayerVoxelShape.Column, pantsShade, unit, ref seed);
            Add(mesh, new Vector3Int(x0, calfRow + 1, 3), 5, LegCalfHeight + LegThighHeight - 1, 1, PlayerVoxelShape.Box, palette.Armor, unit, ref seed);
            Add(mesh, new Vector3Int(x0 + 1, thighRow + LegThighHeight - 1, 4), 3, 1, 1, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seed);
        }

        private static void AddArm(
            PlayerVoxelMeshData mesh,
            int x0,
            int armWidth,
            int torsoTopRow,
            SwordsmanPalette palette,
            PauldronStyle pauldron,
            float unit,
            ref int seed
        )
        {
            PlayerVoxelShape pauldronShape = pauldron == PauldronStyle.Spiky
                ? PlayerVoxelShape.ConeUp
                : PlayerVoxelShape.Sphere;

            // Teintes alternées clair/sombre entre segments successifs : accentue chaque
            // articulation (coude, poignet) au lieu de lisser tout le bras en un seul tube uni.
            Color32 armorDark = Darken(palette.Armor, 0.82f);
            Color32 tunicDark = Darken(palette.Tunic, 0.82f);

            // Bras court et compact, empilé vers le haut à partir de torsoTopRow pour que le
            // pauldron rejoigne exactement la base de la tête (silhouette trapue).
            const int shoulderHeight = 3;
            const int sleeveHeight = 3;
            const int elbowHeight = 2;
            const int forearmHeight = 3;
            const int gauntletHeight = 2;
            const int pauldronHeight = 4;

            int shoulderRow = torsoTopRow - 1;
            int rimRow = shoulderRow + shoulderHeight - 1;
            int pauldronRow = rimRow + 1;
            int sleeveRow = shoulderRow - sleeveHeight + 1;
            int elbowRow = sleeveRow - elbowHeight + 1;
            int forearmRow = elbowRow - forearmHeight + 1;
            int gauntletRow = forearmRow - gauntletHeight + 1;
            int fingerRow = gauntletRow - 1;

            // Pauldron (silhouette signature, forme pilotée par le seed), bras segmenté, gantelet et doigts.
            Add(mesh, new Vector3Int(x0, shoulderRow, -2), armWidth, shoulderHeight, 5, PlayerVoxelShape.Box, palette.Armor, unit, ref seed);
            // Liseré clair juste sous le pauldron : casse la transition plate armure -> épaule.
            Add(mesh, new Vector3Int(x0, rimRow, -2), armWidth, 1, 5, PlayerVoxelShape.Box, palette.ArmorLight, unit, ref seed);
            Add(mesh, new Vector3Int(x0, pauldronRow, -2), armWidth, pauldronHeight, 5, pauldronShape, palette.ArmorLight, unit, ref seed);
            Add(mesh, new Vector3Int(x0, sleeveRow, -2), armWidth, sleeveHeight, 5, PlayerVoxelShape.Column, palette.Tunic, unit, ref seed);
            Add(mesh, new Vector3Int(x0, elbowRow, -2), armWidth, elbowHeight, 5, PlayerVoxelShape.Sphere, armorDark, unit, ref seed);
            Add(mesh, new Vector3Int(x0, forearmRow, -2), armWidth, forearmHeight, 5, PlayerVoxelShape.Column, tunicDark, unit, ref seed);
            Add(mesh, new Vector3Int(x0, gauntletRow, -2), armWidth, gauntletHeight, 5, PlayerVoxelShape.Box, palette.Armor, unit, ref seed);
            Add(mesh, new Vector3Int(x0, fingerRow, 0), 1, 1, 1, PlayerVoxelShape.Box, palette.Skin, unit, ref seed);
            Add(mesh, new Vector3Int(x0 + armWidth - 1, fingerRow, 0), 1, 1, 1, PlayerVoxelShape.Box, palette.Skin, unit, ref seed);
        }

        /// <summary>
        /// Tête dessinée comme un empilement de disques (un par rangée), chaque disque ayant le
        /// rayon donné par <see cref="HeadProfile.RowWidth"/>. Contrairement à un simple pavé
        /// aminci sur les coins, ça donne un vrai galbe 3D (mâchoire étroite, joues bombées,
        /// crâne qui se referme) visible de face ET de profil, pas seulement en profondeur.
        /// </summary>
        private static void AddHead(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            SwordsmanPalette palette,
            CharacterExpression expression,
            float unit,
            ref int seed
        )
        {
            ExpressionShape shape = ExpressionShape.For(expression);
            int headSize = profile.HeadSize;
            float center = (headSize - 1) / 2f;

            for (int row = 0; row < profile.RowWidth.Length; row++)
            {
                int width = profile.RowWidth[row];
                float radius = Mathf.Max(1f, width / 2f);

                for (int lx = 0; lx < headSize; lx++)
                {
                    float dx = (lx - center) / radius;
                    if (dx * dx > 1f)
                    {
                        continue;
                    }

                    float dzMax = radius * Mathf.Sqrt(Mathf.Max(0f, 1f - (dx * dx)));
                    int zFront = Mathf.RoundToInt(center + dzMax);
                    int zBack = Mathf.RoundToInt(center - dzMax);

                    for (int lz = zBack; lz <= zFront; lz++)
                    {
                        if (lz < 0 || lz >= headSize)
                        {
                            continue;
                        }

                        Color32 color = HeadVoxelColor(profile, palette, shape, row, lx, center, width, lz == zFront);
                        AddVoxel(mesh, new Vector3Int(origin.x + lx, origin.y + row, origin.z + lz), color, unit, ref seed);
                    }
                }
            }

            if (!shape.EyesClosed)
            {
                AddEyeSparkles(mesh, origin, profile, palette, unit, ref seed);
            }
        }

        /// <summary>
        /// Couleur d'un voxel de tête : peau/cheveux en base, traits du visage peints sur le
        /// voxel le plus avancé de la rangée (<paramref name="isFrontVoxel"/>). Les yeux ont un
        /// blanc (sclère) autour de l'iris pour éviter l'effet "carré plat". Les traits varient
        /// selon <paramref name="shape"/>.
        /// </summary>
        private static Color32 HeadVoxelColor(
            HeadProfile profile,
            SwordsmanPalette palette,
            ExpressionShape shape,
            int row,
            int localX,
            float center,
            int width,
            bool isFrontVoxel
        )
        {
            Color32 baseColor = row >= profile.HairRowStart ? palette.Hair : palette.Skin;

            if (!isFrontVoxel)
            {
                return baseColor;
            }

            float dist = Mathf.Abs(localX - center);

            if (row == profile.EyeRow)
            {
                float eyeCenter = width * 0.20f;
                float outerHalf = Mathf.Max(0.9f, width * 0.11f * shape.EyeWidthMul);
                float innerHalf = outerHalf * 0.5f;

                if (dist >= eyeCenter - outerHalf && dist <= eyeCenter + outerHalf)
                {
                    if (shape.EyesClosed)
                    {
                        return Darken(palette.Skin, 0.55f);
                    }

                    bool isIris = dist >= eyeCenter - innerHalf && dist <= eyeCenter + innerHalf;
                    return isIris ? palette.Eye : palette.EyeWhite;
                }
            }

            int eyebrowRow = profile.EyeRow + Mathf.Max(2, Mathf.RoundToInt(profile.RowWidth.Length * 0.12f));
            if ((shape.EyebrowsAngry || shape.EyebrowsSad) && row == eyebrowRow && eyebrowRow < profile.HairRowStart)
            {
                float browCenter = width * 0.20f;
                float browHalf = Mathf.Max(0.6f, width * 0.09f);
                if (dist >= browCenter - browHalf && dist <= browCenter + browHalf)
                {
                    return Darken(palette.Hair, 0.6f);
                }
            }

            if (row == profile.BlushRow)
            {
                float blushCenter = width * 0.34f;
                float blushHalf = Mathf.Max(0.5f, width * 0.06f);
                if (dist >= blushCenter - blushHalf && dist <= blushCenter + blushHalf)
                {
                    return palette.Blush;
                }
            }

            int mouthRow = Mathf.Clamp(profile.MouthRow + shape.MouthRowOffset, 0, profile.HairRowStart - 1);
            if (row == mouthRow)
            {
                float mouthHalf = Mathf.Max(0.5f, width * 0.15f * shape.MouthWidthMul);
                if (dist <= mouthHalf)
                {
                    return palette.Mouth;
                }
            }

            return baseColor;
        }

        /// <summary>Éclats blancs dans le regard, placés à la main au-dessus de chaque œil.</summary>
        private static void AddEyeSparkles(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            SwordsmanPalette palette,
            float unit,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            float center = (headSize - 1) / 2f;
            int width = profile.RowWidth[profile.EyeRow];
            float radius = Mathf.Max(1f, width / 2f);
            float offset = width * 0.20f;

            int sparkleRow = profile.EyeRow + 1;
            if (sparkleRow >= profile.HairRowStart || sparkleRow >= profile.RowWidth.Length)
            {
                return;
            }

            // La rangée du sparkle a son propre rayon : on retrouve le bord avant à cet endroit.
            int sparkleRowWidth = profile.RowWidth[sparkleRow];
            float sparkleRadius = Mathf.Max(1f, sparkleRowWidth / 2f);
            float dxNorm = offset / radius;
            float dzMax = sparkleRadius * Mathf.Sqrt(Mathf.Max(0f, 1f - (dxNorm * dxNorm)));
            int sparkleZ = origin.z + Mathf.RoundToInt(center + dzMax);
            int sparkleY = origin.y + sparkleRow;

            AddVoxel(mesh, new Vector3Int(origin.x + Mathf.RoundToInt(center - offset), sparkleY, sparkleZ), palette.EyeWhite, unit, ref seed);
            AddVoxel(mesh, new Vector3Int(origin.x + Mathf.RoundToInt(center + offset), sparkleY, sparkleZ), palette.EyeWhite, unit, ref seed);
        }

        /// <summary>
        /// Remplit un disque plein (une rangée) avec le même calcul de rayon que
        /// <see cref="AddHead"/> : c'est ce qui permet à la base de la coiffure d'épouser
        /// exactement la courbe du crâne, sans jamais laisser de marche ou de vide.
        /// </summary>
        private static void AddDiscRow(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int headSize,
            float center,
            int row,
            float radius,
            Color32 color,
            float unit,
            ref int seed
        )
        {
            for (int lx = 0; lx < headSize; lx++)
            {
                float dx = (lx - center) / radius;
                if (dx * dx > 1f)
                {
                    continue;
                }

                float dzMax = radius * Mathf.Sqrt(Mathf.Max(0f, 1f - (dx * dx)));
                int zFront = Mathf.RoundToInt(center + dzMax);
                int zBack = Mathf.RoundToInt(center - dzMax);

                for (int lz = zBack; lz <= zFront; lz++)
                {
                    if (lz < 0 || lz >= headSize)
                    {
                        continue;
                    }

                    AddVoxel(mesh, new Vector3Int(origin.x + lx, origin.y + row, origin.z + lz), color, unit, ref seed);
                }
            }
        }

        /// <summary>
        /// Rayon de la calotte de cheveux à une rangée donnée. Sous le sommet du crâne (dernière
        /// rangée réelle de <see cref="HeadProfile.RowWidth"/> — le nombre de rangées, pas
        /// <c>HeadSize</c> qui n'est que la largeur), part du rayon RÉEL du crâne à cette rangée
        /// et le multiplie par un facteur de bombé qui vaut exactement 1 aux deux extrémités (bas
        /// de la coiffure ET sommet du crâne) et culmine au milieu — la base est donc TOUJOURS au
        /// moins aussi large que le crâne, ce qui élimine toute marche ou vide visible, quel que
        /// soit le seed. Au-dessus du sommet du crâne, le rayon retombe linéairement à 0 pour
        /// refermer la calotte en pointe.
        /// </summary>
        private static float HairCapRadius(HeadProfile profile, int startRow, int topRow, int row, float bulgeAmount)
        {
            int skullTopRow = profile.RowWidth.Length - 1;

            if (row <= skullTopRow)
            {
                int clampedRow = Mathf.Clamp(row, 0, skullTopRow);
                float skullRadius = Mathf.Max(1f, profile.RowWidth[clampedRow] / 2f);
                float span = Mathf.Max(1, skullTopRow - startRow);
                float t = Mathf.Clamp((row - startRow) / span, 0f, 1f);
                float bulge = 1f + (bulgeAmount * Mathf.Sin(Mathf.PI * t));
                return skullRadius * bulge;
            }

            float tipRadius = Mathf.Max(1f, profile.RowWidth[skullTopRow] / 2f);
            float extraSpan = Mathf.Max(1, topRow - skullTopRow);
            float tExtra = Mathf.Clamp((row - skullTopRow) / extraSpan, 0f, 1f);
            return tipRadius * (1f - tExtra);
        }

        /// <summary>
        /// Calotte de cheveux pleine construite disque par disque avec <see cref="HairCapRadius"/>.
        /// Remplace les anciennes boîtes à bords droits qui reproduisaient une fraction fixe de
        /// <c>headSize</c> sans jamais suivre la courbure réelle du crâne sous-jacent.
        /// </summary>
        private static void AddHairCap(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            int startRow,
            int topRow,
            float bulgeAmount,
            Color32 color,
            float unit,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            float center = (headSize - 1) / 2f;

            for (int row = startRow; row <= topRow; row++)
            {
                float radius = HairCapRadius(profile, startRow, topRow, row, bulgeAmount);
                if (radius < 0.6f)
                {
                    continue;
                }

                AddDiscRow(mesh, origin, headSize, center, row, radius, color, unit, ref seed);
            }
        }

        /// <summary>
        /// Coiffure posée sur le crâne réellement généré, construite à partir de plusieurs mèches
        /// cubiques distinctes (pas un dôme lisse) : une grosse touffe centrale, des mèches
        /// latérales/arrière décalées en camaïeu clair/sombre pour créer du relief, plus les
        /// touches propres à chaque style (épi, crête, plis plaqués).
        /// </summary>
        private static void AddHair(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            HairStyle style,
            Color32 hairColor,
            float unit,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            float center = (headSize - 1) / 2f;
            int rows = profile.RowWidth.Length;
            int hairStart = Mathf.Min(profile.HairRowStart, rows - 1);
            Color32 hairShadow = Darken(hairColor, 0.7f);

            switch (style)
            {
                case HairStyle.Mohawk:
                    AddMohawkHair(mesh, origin, profile, hairColor, hairShadow, unit, center, hairStart, ref seed);
                    break;

                case HairStyle.Slick:
                    AddSlickHair(mesh, origin, profile, hairColor, hairShadow, unit, center, hairStart, ref seed);
                    break;

                case HairStyle.SidePartAhoge:
                default:
                    AddSpikyHair(mesh, origin, profile, hairColor, hairShadow, unit, center, hairStart, ref seed);
                    break;
            }
        }

        /// <summary>
        /// Trouve le voxel Z le plus avancé (surface du crâne) pour une rangée et un décalage en
        /// X depuis le centre donnés, afin de coller une mèche pile sur la peau sans qu'elle
        /// flotte devant ou s'enfonce dedans.
        /// </summary>
        private static int SurfaceFrontZ(HeadProfile profile, float center, int row, float dxFromCenter)
        {
            int clampedRow = Mathf.Clamp(row, 0, profile.RowWidth.Length - 1);
            float radius = Mathf.Max(1f, profile.RowWidth[clampedRow] / 2f);
            float dxNorm = Mathf.Clamp(dxFromCenter / radius, -0.98f, 0.98f);
            float dzMax = radius * Mathf.Sqrt(Mathf.Max(0f, 1f - (dxNorm * dxNorm)));
            return Mathf.RoundToInt(center + dzMax);
        }

        /// <summary>
        /// Coiffure balayée cohérente : une calotte pleine qui épouse le crâne (pas de touffes
        /// éparses qui lisent comme des "mèches" séparées), un petit épi discret au sommet, et
        /// deux mèches qui retombent sur le front. Volontairement peu d'éléments distincts pour
        /// rester lisible comme UNE coiffure plutôt qu'un bouquet de pointes.
        /// </summary>
        private static void AddSpikyHair(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            Color32 hairColor,
            Color32 hairShadow,
            float unit,
            float center,
            int hairStart,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            // Sommet réel du crâne = dernière rangée de RowWidth (le nombre de rangées, pas
            // HeadSize qui n'est que la largeur) : la calotte doit se refermer là, pas plus haut.
            int skullTopRow = profile.RowWidth.Length - 1;

            // La calotte démarre sous la racine des cheveux avec EXACTEMENT le rayon du crâne
            // (aucune marche possible), bombe de ~38% vers le milieu, puis se referme en pointe
            // au-dessus du sommet du crâne — elle épouse la courbure quel que soit le seed.
            const float bulge = 0.38f;
            int startRow = Mathf.Max(0, hairStart - 2);
            int topRow = skullTopRow + Mathf.Max(2, Mathf.RoundToInt(headSize * 0.16f));
            AddHairCap(mesh, origin, profile, startRow, topRow, bulge, hairColor, unit, ref seed);

            // Petit épi discret là où la calotte se referme en pointe (accent, pas une pointe
            // dominante) : reste subtil pour ne pas retomber dans l'effet "plein de mèches".
            int ahogeSize = Mathf.Max(3, Mathf.RoundToInt(headSize * 0.22f));
            int ahogeHeight = Mathf.Max(2, Mathf.RoundToInt(headSize * 0.18f));
            int ahogeX0 = origin.x + Mathf.RoundToInt(center - (ahogeSize / 2f));
            int ahogeZ0 = origin.z + Mathf.RoundToInt(center - (ahogeSize / 2f));
            Add(mesh, new Vector3Int(ahogeX0, origin.y + topRow - 1, ahogeZ0), ahogeSize, ahogeHeight, ahogeSize, PlayerVoxelShape.ConeUp, hairShadow, unit, ref seed);

            // Mèches qui retombent sur le front, collées à la surface réelle du crâne à cette
            // rangée : c'est ce qui vend le look "coiffure balayée" façon référence, pas des
            // touffes latérales supplémentaires.
            int bangRow = Mathf.Clamp(profile.EyeRow + 2, 0, hairStart - 1);
            int bangSize = Mathf.Max(3, Mathf.RoundToInt(headSize * 0.22f));
            float bangDx = headSize * 0.22f;

            int leftBangZ = origin.z + SurfaceFrontZ(profile, center, bangRow, -bangDx) - 1;
            int rightBangZ = origin.z + SurfaceFrontZ(profile, center, bangRow, bangDx) - 1;
            Add(mesh, new Vector3Int(origin.x + Mathf.RoundToInt(center - bangDx - (bangSize / 2f)), origin.y + bangRow, leftBangZ), bangSize, 3, 2, PlayerVoxelShape.Box, hairColor, unit, ref seed);
            Add(mesh, new Vector3Int(origin.x + Mathf.RoundToInt(center + bangDx - (bangSize / 2f)), origin.y + bangRow, rightBangZ), bangSize, 3, 2, PlayerVoxelShape.Box, hairColor, unit, ref seed);
        }

        /// <summary>Crête de segments cubiques jagged le long du centre du crâne, tempes rasées.</summary>
        private static void AddMohawkHair(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            Color32 hairColor,
            Color32 hairShadow,
            float unit,
            float center,
            int hairStart,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            int baseRow = Mathf.Clamp(hairStart - 1, 0, profile.RowWidth.Length - 1);
            float skullRadiusAtBase = Mathf.Max(1f, profile.RowWidth[baseRow] / 2f);

            // Largeur dérivée du rayon RÉEL du crâne à cette rangée (pas une fraction fixe de
            // headSize) : la base de la crête épouse le haut du crâne quel que soit le seed.
            int ridgeWidth = Mathf.Max(3, Mathf.RoundToInt(skullRadiusAtBase * 1.05f));
            int ridgeX0 = origin.x + Mathf.RoundToInt(center - (ridgeWidth / 2f));
            int ridgeY = origin.y + baseRow;

            // Base de la crête : une bande qui court sur toute la profondeur du crâne.
            Add(mesh, new Vector3Int(ridgeX0, ridgeY, origin.z), ridgeWidth, 2, headSize, PlayerVoxelShape.Box, hairShadow, unit, ref seed);

            // Segments jagged empilés dessus, hauteur alternée pour une silhouette de crête nette.
            int segmentCount = 5;
            int segmentWidth = Mathf.Max(2, ridgeWidth - 1);
            int segmentX0 = origin.x + Mathf.RoundToInt(center - (segmentWidth / 2f));
            int tallHeight = Mathf.Max(4, Mathf.RoundToInt(headSize * 0.34f));
            int shortHeight = Mathf.Max(2, Mathf.RoundToInt(headSize * 0.18f));

            for (int i = 0; i < segmentCount; i++)
            {
                float t = (i + 0.5f) / segmentCount;
                int segmentZ = origin.z + Mathf.RoundToInt((headSize - 2) * t);
                int segmentHeight = i % 2 == 0 ? tallHeight : shortHeight;
                Color32 segmentColor = i % 2 == 0 ? hairColor : hairShadow;
                Add(mesh, new Vector3Int(segmentX0, ridgeY + 2, segmentZ), segmentWidth, segmentHeight, 2, PlayerVoxelShape.Box, segmentColor, unit, ref seed);
            }
        }

        /// <summary>Cheveux plaqués en arrière : calotte fine et quasi plate + plis qui se chevauchent comme des tuiles.</summary>
        private static void AddSlickHair(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            HeadProfile profile,
            Color32 hairColor,
            Color32 hairShadow,
            float unit,
            float center,
            int hairStart,
            ref int seed
        )
        {
            int headSize = profile.HeadSize;
            // Sommet réel du crâne = dernière rangée de RowWidth (nombre de rangées), pas
            // HeadSize qui n'est que la largeur.
            int skullTopRow = profile.RowWidth.Length - 1;

            // Calotte peu bombée ("plaquée") mais qui part quand même du rayon réel du crâne :
            // colle à la tête sans jamais laisser de marche, contrairement aux anciens plis
            // dont la largeur fixe ne suivait pas la courbure sous-jacente.
            const float bulge = 0.08f;
            int startRow = Mathf.Max(0, hairStart - 2);
            int topRow = skullTopRow + Mathf.Max(1, Mathf.RoundToInt(headSize * 0.06f));
            AddHairCap(mesh, origin, profile, startRow, topRow, bulge, hairColor, unit, ref seed);

            // Plis/tuiles qui se chevauchent, dimensionnés sur le rayon réel à chaque rangée
            // pour rester visuellement posés SUR la calotte plutôt que de flotter au-dessus.
            int plateCount = 3;
            for (int i = 0; i < plateCount; i++)
            {
                int plateRow = startRow + Mathf.RoundToInt((skullTopRow - startRow) * (0.25f + (i * 0.28f)));
                float plateRadius = HairCapRadius(profile, startRow, topRow, plateRow, bulge);
                int plateWidth = Mathf.Max(3, Mathf.RoundToInt(plateRadius * 1.7f));
                int plateHeight = Mathf.Max(1, Mathf.RoundToInt(headSize * 0.08f));
                int plateDepth = Mathf.Max(2, Mathf.RoundToInt(plateRadius * 0.9f));
                int plateX0 = origin.x + Mathf.RoundToInt(center - (plateWidth / 2f));
                int plateZ0 = origin.z + Mathf.RoundToInt(center - (plateDepth / 2f)) - i;
                Color32 plateColor = i == plateCount - 1 ? hairShadow : hairColor;
                Add(mesh, new Vector3Int(plateX0, origin.y + plateRow, plateZ0), plateWidth, plateHeight, plateDepth, PlayerVoxelShape.Box, plateColor, unit, ref seed);
            }

            // Petite mèche de côté qui accroche la lumière, pour éviter l'effet "casque uni".
            int sideSize = Mathf.Max(2, Mathf.RoundToInt(headSize * 0.16f));
            int sideBangRow = Mathf.Clamp(profile.EyeRow + 1, 0, hairStart - 1);
            float sideDx = headSize * 0.30f;
            int sideZ = origin.z + SurfaceFrontZ(profile, center, sideBangRow, sideDx) - 1;
            Add(mesh, new Vector3Int(origin.x + Mathf.RoundToInt(center + sideDx - (sideSize / 2f)), origin.y + sideBangRow, sideZ), sideSize, 2, 2, PlayerVoxelShape.Box, hairShadow, unit, ref seed);
        }

        private static Color32 Darken(Color32 color, float factor) =>
            new(
                (byte)(color.r * factor),
                (byte)(color.g * factor),
                (byte)(color.b * factor),
                color.a
            );

        private static int ScaleLen(int baseLen, float scale) => Mathf.Max(1, Mathf.RoundToInt(baseLen * scale));

        private static void AddVoxel(PlayerVoxelMeshData mesh, Vector3Int origin, Color32 color, float unit, ref int seed) =>
            Add(mesh, origin, 1, 1, 1, PlayerVoxelShape.Box, color, unit, ref seed);

        private static void Add(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            PlayerVoxelShape shape,
            Color32 color,
            float unit,
            ref int seed
        ) => PlayerVoxelMeshCore.AddPart(mesh, origin, sx, sy, sz, shape, color, unit, seed++);

        /// <summary>Traduit une <see cref="CharacterExpression"/> en paramètres de dessin du visage.</summary>
        private readonly struct ExpressionShape
        {
            public readonly bool EyesClosed;
            public readonly float EyeWidthMul;
            public readonly float MouthWidthMul;
            public readonly int MouthRowOffset;
            public readonly bool EyebrowsAngry;
            public readonly bool EyebrowsSad;

            private ExpressionShape(
                bool eyesClosed,
                float eyeWidthMul,
                float mouthWidthMul,
                int mouthRowOffset,
                bool eyebrowsAngry,
                bool eyebrowsSad
            )
            {
                EyesClosed = eyesClosed;
                EyeWidthMul = eyeWidthMul;
                MouthWidthMul = mouthWidthMul;
                MouthRowOffset = mouthRowOffset;
                EyebrowsAngry = eyebrowsAngry;
                EyebrowsSad = eyebrowsSad;
            }

            public static ExpressionShape For(CharacterExpression expression) => expression switch
            {
                CharacterExpression.Happy => new ExpressionShape(false, 0.9f, 1.7f, 1, false, false),
                CharacterExpression.Angry => new ExpressionShape(false, 0.8f, 0.9f, 0, true, false),
                CharacterExpression.Sad => new ExpressionShape(false, 0.9f, 0.7f, -1, false, true),
                CharacterExpression.Surprised => new ExpressionShape(false, 1.6f, 0.4f, 0, false, false),
                CharacterExpression.Blink => new ExpressionShape(true, 1f, 1f, 0, false, false),
                _ => new ExpressionShape(false, 1f, 1f, 0, false, false),
            };
        }
    }
}
