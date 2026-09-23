using CubeWorld.CharacterModel.Core;
using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel.Parts
{
    /// <summary>
    /// Tête chibi façon Link : boîte rectangulaire à coins adoucis (sections "squircle"
    /// empilées, largeur par rangée donnée par <see cref="HeadShape"/>), visage peint sur
    /// les voxels frontaux (yeux sclère+iris+éclat, bouche, blush, sourcils selon
    /// l'expression), oreilles pointues (selon l'archétype), bande de cheveux (frange +
    /// mèches qui encadrent le visage + nuque) et couvre-tête selon <see cref="CapStyle"/>
    /// (bonnet pointu, chapeau de sorcier, ou aucun). Pivot à la base du cou : la tête
    /// s'anime en la faisant pivoter là.
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : nouveaux styles de couvre-tête dans
    /// <see cref="CapStyle"/> + le switch de StampHairAndCap ; variantes de bonnet dans
    /// <see cref="HairStyle"/> ; galbe dans HeadShape.Generate ; taille des oreilles dans
    /// StampEars ; accessoires (couronne, plume...) à stamper ici, dans la même grille.
    /// </summary>
    internal static class HeadGenerator
    {
        /// <summary>Rangées de cou sous le menton (la 1re s'encastre dans le torse).</summary>
        private const int NeckRows = 2;

        /// <summary>Marge de grille autour du crâne (oreilles + débord du bonnet).</summary>
        private const int HairMargin = 3;

        /// <summary>Rangées de cheveux visibles sous le bord du bonnet.</summary>
        private const int HairBandRows = 2;

        public static BodyPart Build(
            CharacterBodyParams body,
            HeadShape shape,
            CharacterPalette palette,
            CharacterExpression expression,
            CharacterSilhouette silhouette,
            int capRows,
            float unit,
            int colorSeed
        )
        {
            // Le sorcier porte un chapeau plus haut que le bonnet snug du héros ; la tête nue
            // (CapStyle.None, ex. l'archer) n'a besoin d'aucune rangée de couvre-tête.
            int effectiveCapRows = silhouette.Cap switch
            {
                CapStyle.None => 0,
                CapStyle.Wizard => Mathf.RoundToInt(capRows * 1.6f),
                _ => capRows,
            };

            int gridSize = body.HeadWidth + (HairMargin * 2);
            int rows = shape.RowWidth.Length;
            int gridHeight = NeckRows + rows + effectiveCapRows + 1;

            var part = new VoxelPartMeshBuilder(
                gridSize,
                gridHeight,
                gridSize,
                new Vector3(gridSize / 2f, 0f, gridSize / 2f),
                unit
            );
            VoxelGrid grid = part.Grid;
            float center = (gridSize - 1) / 2f;
            ExpressionShape face = ExpressionShape.For(expression);

            StampNeck(grid, shape, gridSize, palette);
            StampSkullAndFace(grid, shape, palette, face, silhouette, center, gridSize);

            if (silhouette.HasPointedEars)
            {
                StampEars(grid, shape, palette, center);
            }

            if (silhouette.HasHair || silhouette.Cap != CapStyle.None)
            {
                StampHairAndCap(
                    grid,
                    body,
                    shape,
                    palette,
                    silhouette,
                    center,
                    gridSize,
                    effectiveCapRows
                );
            }

            if (silhouette.HasEyes)
            {
                if (!face.EyesClosed)
                {
                    PaintEyeSparkles(grid, shape, palette, center);
                }
            }
            else
            {
                CarveEyeSockets(grid, shape, palette, center, gridSize);
            }

            if (silhouette.HasSkullFace)
            {
                CarveNasalCavity(grid, shape, palette, center, gridSize);
                CarveJaw(grid, shape, face, palette, center, gridSize);
            }

            Mesh mesh = part.ToMesh("CharacterHead", colorSeed);
            return new BodyPart(CharacterRigDefinition.Head, mesh);
        }

        /// <summary>
        /// Demi-profondeur d'une section "squircle" (|x|^4 + |z|^4 = r^4) : c'est elle qui
        /// rend la tête rectangulaire — les flancs restent presque à pleine profondeur
        /// jusqu'aux coins, contrairement à un disque qui s'arrondit tout de suite.
        /// </summary>
        private static float SquircleHalfDepth(float radius, float dxNorm)
        {
            float d4 = dxNorm * dxNorm * dxNorm * dxNorm;
            return radius * Mathf.Pow(Mathf.Max(0f, 1f - d4), 0.25f);
        }

        /// <summary>
        /// Cou : colonne courte et large (chibi), la rangée 0 s'encastre dans le haut du torse
        /// pour couvrir le joint quand la tête pivote. Largeur ajustée à la parité de la
        /// grille pour rester parfaitement centrée.
        /// </summary>
        private static void StampNeck(
            VoxelGrid grid,
            HeadShape shape,
            int gridSize,
            CharacterPalette palette
        )
        {
            int neckWidth = MatchParity(gridSize, Mathf.RoundToInt(shape.HeadWidth * 0.45f));
            int origin = (gridSize - neckWidth) / 2;

            VoxelStamper.Stamp(
                grid,
                new Vector3Int(origin, 0, origin),
                neckWidth,
                NeckRows + 1,
                neckWidth,
                VoxelShape.Column,
                palette.SkinShadow
            );
        }

        /// <summary>
        /// Crâne : une section squircle pleine par rangée. Les traits du visage sont peints à
        /// la volée sur le voxel le plus avancé (+Z) de chaque colonne — la "peau" du visage
        /// suit exactement le galbe du crâne.
        /// </summary>
        private static void StampSkullAndFace(
            VoxelGrid grid,
            HeadShape shape,
            CharacterPalette palette,
            ExpressionShape face,
            CharacterSilhouette silhouette,
            float center,
            int gridSize
        )
        {
            for (int row = 0; row < shape.RowWidth.Length; row++)
            {
                int width = shape.RowWidth[row];
                float radius = Mathf.Max(1f, width / 2f);

                for (int lx = 0; lx < gridSize; lx++)
                {
                    float dx = (lx - center) / radius;
                    if (dx * dx > 1f)
                    {
                        continue;
                    }

                    float dzMax = SquircleHalfDepth(radius, dx);
                    int zFront = Mathf.RoundToInt(center + dzMax);
                    int zBack = Mathf.RoundToInt(center - dzMax);

                    for (int lz = zBack; lz <= zFront; lz++)
                    {
                        Color32 color = FaceColor(
                            shape,
                            palette,
                            face,
                            silhouette,
                            row,
                            lx,
                            center,
                            width,
                            lz == zFront
                        );
                        grid.Set(lx, NeckRows + row, lz, color);
                    }
                }
            }
        }

        /// <summary>
        /// Couleur d'un voxel de crâne : peau en base, cheveux blonds sur les rangées hautes
        /// (la bande qui restera visible sous le bord du bonnet), traits du visage uniquement
        /// sur le voxel frontal.
        /// </summary>
        private static Color32 FaceColor(
            HeadShape shape,
            CharacterPalette palette,
            ExpressionShape face,
            CharacterSilhouette silhouette,
            int row,
            int localX,
            float center,
            int width,
            bool isFrontVoxel
        )
        {
            Color32 baseColor =
                row >= shape.HairRowStart - HairBandRows ? palette.Hair : palette.Skin;

            if (!isFrontVoxel)
            {
                return baseColor;
            }

            float dist = Mathf.Abs(localX - center);

            if (silhouette.HasEyes && row == shape.EyeRow)
            {
                float eyeCenter = width * 0.20f;
                float outerHalf = Mathf.Max(0.9f, width * 0.11f * face.EyeWidthMul);
                float innerHalf = outerHalf * 0.5f;

                if (dist >= eyeCenter - outerHalf && dist <= eyeCenter + outerHalf)
                {
                    if (face.EyesClosed)
                    {
                        return CharacterPalette.Darken(palette.Skin, 0.55f);
                    }

                    bool isIris = dist >= eyeCenter - innerHalf && dist <= eyeCenter + innerHalf;
                    return isIris ? palette.Eye : palette.EyeWhite;
                }
            }

            // Sourcils peints seulement si la silhouette a des cheveux : sans eux, le regard
            // perd son contraste et le visage lit "flou" à distance (mais un crâne n'a pas de
            // sourcils à peindre).
            int eyebrowRow =
                shape.EyeRow + Mathf.Max(2, Mathf.RoundToInt(shape.RowWidth.Length * 0.12f));
            if (silhouette.HasHair && row == eyebrowRow && eyebrowRow < shape.HairRowStart)
            {
                float browCenter = width * 0.20f;
                float browHalf = Mathf.Max(0.6f, width * 0.09f);
                if (dist >= browCenter - browHalf && dist <= browCenter + browHalf)
                {
                    return palette.HairShadow;
                }
            }

            if (silhouette.HasBlush && row == shape.BlushRow)
            {
                float blushCenter = width * 0.34f;
                float blushHalf = Mathf.Max(0.5f, width * 0.06f);
                if (dist >= blushCenter - blushHalf && dist <= blushCenter + blushHalf)
                {
                    return palette.Blush;
                }
            }

            int mouthRow = Mathf.Clamp(
                shape.MouthRow + face.MouthRowOffset,
                0,
                shape.HairRowStart - 1
            );
            if (row == mouthRow)
            {
                float mouthHalf = Mathf.Max(0.5f, width * 0.15f * face.MouthWidthMul);
                if (dist <= mouthHalf)
                {
                    return palette.Mouth;
                }
            }

            return baseColor;
        }

        /// <summary>
        /// Oreilles pointues d'elfe : une base 2x2 collée au flanc du crâne à hauteur des
        /// yeux, prolongée d'une pointe d'un voxel légèrement plus haute — lisible de face
        /// comme de profil.
        /// </summary>
        private static void StampEars(
            VoxelGrid grid,
            HeadShape shape,
            CharacterPalette palette,
            float center
        )
        {
            int earRow = NeckRows + shape.EyeRow;
            float radius = Mathf.Max(1f, shape.RowWidth[shape.EyeRow] / 2f);
            int zCenter = Mathf.RoundToInt(center);

            int rightBase = Mathf.RoundToInt(center + radius);
            int leftBase = Mathf.RoundToInt(center - radius);

            for (int dy = 0; dy < 2; dy++)
            {
                for (int dz = -1; dz <= 0; dz++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        grid.Set(rightBase + i, earRow + dy, zCenter + dz, palette.Skin);
                        grid.Set(leftBase - i, earRow + dy, zCenter + dz, palette.Skin);
                    }
                }
            }

            // Pointes, décalées vers le haut et l'arrière.
            grid.Set(rightBase + 2, earRow + 1, zCenter - 1, palette.Skin);
            grid.Set(leftBase - 2, earRow + 1, zCenter - 1, palette.Skin);
        }

        /// <summary>Bande de cheveux, frange, mèches latérales, puis le couvre-tête par-dessus (voir <see cref="CapStyle"/>).</summary>
        private static void StampHairAndCap(
            VoxelGrid grid,
            CharacterBodyParams body,
            HeadShape shape,
            CharacterPalette palette,
            CharacterSilhouette silhouette,
            float center,
            int gridSize,
            int capRows
        )
        {
            // Frange sur le front, sous le bord du bonnet (asymétrique pour SideSwept),
            // et mèches qui descendent devant les oreilles pour encadrer le visage.
            if (body.Hair == HairStyle.SideSwept)
            {
                PaintHairStrip(
                    grid,
                    shape,
                    palette.Hair,
                    center,
                    shape.EyeRow + 2,
                    shape.HairRowStart - 1,
                    -0.42f,
                    0.2f
                );
                PaintHairStrip(
                    grid,
                    shape,
                    palette.Hair,
                    center,
                    shape.EyeRow + 1,
                    shape.HairRowStart - 1,
                    0.05f,
                    0.45f
                );
            }
            else
            {
                PaintHairStrip(
                    grid,
                    shape,
                    palette.Hair,
                    center,
                    shape.EyeRow + 2,
                    shape.HairRowStart - 1,
                    -0.42f,
                    0.42f
                );
            }

            PaintHairStrip(
                grid,
                shape,
                palette.Hair,
                center,
                shape.EyeRow - 1,
                shape.HairRowStart - 1,
                -0.52f,
                -0.40f
            );
            PaintHairStrip(
                grid,
                shape,
                palette.Hair,
                center,
                shape.EyeRow - 1,
                shape.HairRowStart - 1,
                0.40f,
                0.52f
            );

            if (silhouette.Cap == CapStyle.None)
            {
                // Tête nue (ex. l'archer) : seule la bande de cheveux ci-dessus reste
                // visible, aucune rangée de couvre-tête à stamper.
                return;
            }

            // Bonnet snug qui recouvre le haut du crâne (déborde d'un demi-voxel sur la
            // bande de cheveux) — base commune aux deux styles de couvre-tête.
            bool tallCap = body.Hair == HairStyle.TallCap;
            float droop = tallCap ? 0.2f : 0.5f;
            int skullTopRow = shape.RowWidth.Length - 1;

            for (int row = shape.HairRowStart; row <= skullTopRow; row++)
            {
                float radius = Mathf.Max(1f, shape.RowWidth[row] / 2f) + 0.6f;
                FillCapRow(grid, row, radius, center, 0f, gridSize, palette.OutfitPrimary);
            }

            float baseRadius = Mathf.Max(1f, shape.RowWidth[skullTopRow] / 2f) + 0.6f;

            if (silhouette.Cap == CapStyle.Wizard)
            {
                // Large bord évasé qui casse nettement la silhouette avant le cône —
                // signature du chapeau de sorcier, bien plus large que le bonnet snug du
                // héros — puis cône haut qui se resserre jusqu'à une pointe fine.
                FillCapRow(
                    grid,
                    skullTopRow + 1,
                    baseRadius + 1.8f,
                    center,
                    0f,
                    gridSize,
                    palette.OutfitPrimary
                );

                for (int i = 2; i <= capRows; i++)
                {
                    float t = (float)(i - 1) / Mathf.Max(1, capRows - 1);
                    float radius = Mathf.Lerp(baseRadius, 0.5f, t);
                    float zOffset = -droop * (i - 1);
                    FillCapRow(
                        grid,
                        skullTopRow + i,
                        radius,
                        center,
                        zOffset,
                        gridSize,
                        palette.OutfitPrimary
                    );
                }

                return;
            }

            // Cône pointu incliné vers l'arrière, silhouette héros/elfe classique.
            for (int i = 1; i <= capRows; i++)
            {
                float t = (float)i / capRows;
                float radius = Mathf.Lerp(baseRadius, 0.8f, t);
                float zOffset = -droop * i;
                FillCapRow(
                    grid,
                    skullTopRow + i,
                    radius,
                    center,
                    zOffset,
                    gridSize,
                    palette.OutfitPrimary
                );
            }
        }

        /// <summary>Une rangée de bonnet : squircle plein, centre décalable en Z (inclinaison).</summary>
        private static void FillCapRow(
            VoxelGrid grid,
            int skullRow,
            float radius,
            float center,
            float zOffset,
            int gridSize,
            Color32 color
        )
        {
            for (int lx = 0; lx < gridSize; lx++)
            {
                float dx = (lx - center) / radius;
                if (dx * dx > 1f)
                {
                    continue;
                }

                float dzMax = SquircleHalfDepth(radius, dx);
                int zFront = Mathf.RoundToInt(center + zOffset + dzMax);
                int zBack = Mathf.RoundToInt(center + zOffset - dzMax);

                for (int lz = zBack; lz <= zFront; lz++)
                {
                    grid.Set(lx, NeckRows + skullRow, lz, color);
                }
            }
        }

        /// <summary>
        /// Peint une bande de cheveux sur la surface AVANT du crâne (recolore le voxel
        /// frontal + 1 voxel de débord pour l'épaisseur), entre deux rangées et sur une
        /// plage horizontale en fractions de la largeur de rangée (négatif = gauche).
        /// </summary>
        private static void PaintHairStrip(
            VoxelGrid grid,
            HeadShape shape,
            Color32 color,
            float center,
            int rowFrom,
            int rowTo,
            float minDx,
            float maxDx
        )
        {
            rowFrom = Mathf.Clamp(rowFrom, 0, shape.RowWidth.Length - 1);
            rowTo = Mathf.Clamp(rowTo, 0, shape.RowWidth.Length - 1);

            for (int row = rowFrom; row <= rowTo; row++)
            {
                int width = shape.RowWidth[row];
                float radius = Mathf.Max(1f, width / 2f);

                for (float dxFrac = minDx; dxFrac <= maxDx; dxFrac += 1f / Mathf.Max(1f, width))
                {
                    int lx = Mathf.RoundToInt(center + (dxFrac * width));
                    float dx = (lx - center) / radius;
                    if (dx * dx > 0.98f)
                    {
                        continue;
                    }

                    float dzMax = SquircleHalfDepth(radius, dx);
                    int zFront = Mathf.RoundToInt(center + dzMax);
                    int y = NeckRows + row;

                    grid.Set(lx, y, zFront, color);
                    grid.Set(lx, y, zFront + 1, color);
                }
            }
        }

        /// <summary>Éclat blanc dans le regard, recoloré sur la surface au-dessus de chaque œil.</summary>
        private static void PaintEyeSparkles(
            VoxelGrid grid,
            HeadShape shape,
            CharacterPalette palette,
            float center
        )
        {
            int sparkleRow = shape.EyeRow + 1;
            if (sparkleRow >= shape.HairRowStart || sparkleRow >= shape.RowWidth.Length)
            {
                return;
            }

            float offset = shape.RowWidth[shape.EyeRow] * 0.20f;

            int y = NeckRows + sparkleRow;
            grid.PaintFrontmost(Mathf.RoundToInt(center - offset), y, palette.EyeWhite);
            grid.PaintFrontmost(Mathf.RoundToInt(center + offset), y, palette.EyeWhite);
        }

        /// <summary>
        /// Orbites creuses (squelette) : au lieu de peindre un aplat, on VIDE le voxel frontal
        /// de la zone des yeux (alpha 0 via <see cref="VoxelGrid.Clear"/>) et on recolore le
        /// voxel nouvellement exposé derrière avec <see cref="CharacterPalette.EyeSocket"/> —
        /// un vrai creux, pas juste un dessin sur la surface.
        /// </summary>
        private static void CarveEyeSockets(
            VoxelGrid grid,
            HeadShape shape,
            CharacterPalette palette,
            float center,
            int gridSize
        )
        {
            int row = shape.EyeRow;
            int width = shape.RowWidth[row];
            float eyeCenter = width * 0.20f;
            float outerHalf = Mathf.Max(0.9f, width * 0.11f);
            int y = NeckRows + row;

            int minOffset = Mathf.FloorToInt(eyeCenter - outerHalf);
            int maxOffset = Mathf.CeilToInt(eyeCenter + outerHalf);

            for (int offset = minOffset; offset <= maxOffset; offset++)
            {
                CarveFrontmost(grid, Mathf.RoundToInt(center - offset), y, gridSize, palette);
                CarveFrontmost(grid, Mathf.RoundToInt(center + offset), y, gridSize, palette);
            }
        }

        /// <summary>
        /// Cavité nasale (squelette) : petite fosse creusée entre les orbites et la bouche —
        /// 1 voxel en haut, 3 en bas (léger évasement façon ouverture nasale), même technique
        /// de vrai trou que <see cref="CarveEyeSockets"/>. Silencieusement ignorée si la tête
        /// est trop petite pour dégager assez de place entre les yeux et la bouche.
        /// </summary>
        private static void CarveNasalCavity(
            VoxelGrid grid,
            HeadShape shape,
            CharacterPalette palette,
            float center,
            int gridSize
        )
        {
            int rowTop = shape.EyeRow - 2;
            int rowBottom = rowTop - 1;

            if (rowBottom <= shape.MouthRow || rowTop < 0)
            {
                return;
            }

            int x = Mathf.RoundToInt(center);
            CarveFrontmost(grid, x, NeckRows + rowTop, gridSize, palette);

            for (int dx = -1; dx <= 1; dx++)
            {
                CarveFrontmost(grid, x + dx, NeckRows + rowBottom, gridSize, palette);
            }
        }

        /// <summary>
        /// Mâchoire à dents (squelette) : recreuse la bande de bouche déjà peinte par
        /// <see cref="FaceColor"/> en alternant dents claires (voxel repeint, resté solide) et
        /// interstices creusés (vrai trou, même technique que <see cref="CarveEyeSockets"/>) —
        /// beaucoup plus lisible qu'un aplat sombre uni pour lire "mâchoire de squelette".
        /// </summary>
        private static void CarveJaw(
            VoxelGrid grid,
            HeadShape shape,
            ExpressionShape face,
            CharacterPalette palette,
            float center,
            int gridSize
        )
        {
            int mouthRow = Mathf.Clamp(
                shape.MouthRow + face.MouthRowOffset,
                0,
                shape.HairRowStart - 1
            );
            int width = shape.RowWidth[mouthRow];
            float mouthHalf = Mathf.Max(0.5f, width * 0.15f * face.MouthWidthMul);
            int y = NeckRows + mouthRow;

            int minOffset = Mathf.CeilToInt(-mouthHalf);
            int maxOffset = Mathf.FloorToInt(mouthHalf);

            for (int offset = minOffset; offset <= maxOffset; offset++)
            {
                int lx = Mathf.RoundToInt(center + offset);
                bool isGap = (offset - minOffset) % 2 == 0;

                if (isGap)
                {
                    CarveFrontmost(grid, lx, y, gridSize, palette);
                }
                else
                {
                    grid.PaintFrontmost(lx, y, palette.Skin);
                }
            }
        }

        /// <summary>Vide le voxel le plus avancé (+Z) d'une colonne et peint le fond exposé.</summary>
        private static void CarveFrontmost(
            VoxelGrid grid,
            int x,
            int y,
            int gridSize,
            CharacterPalette palette
        )
        {
            for (int z = gridSize - 1; z >= 0; z--)
            {
                if (!grid.IsSolid(x, y, z))
                {
                    continue;
                }

                grid.Clear(x, y, z);

                if (grid.IsSolid(x, y, z - 1))
                {
                    grid.Set(x, y, z - 1, palette.EyeSocket);
                }

                return;
            }
        }

        /// <summary>Ajuste la parité d'une largeur pour qu'elle se centre exactement dans la grille.</summary>
        private static int MatchParity(int gridSize, int width) =>
            (gridSize - width) % 2 == 0 ? width : width + 1;
    }
}
