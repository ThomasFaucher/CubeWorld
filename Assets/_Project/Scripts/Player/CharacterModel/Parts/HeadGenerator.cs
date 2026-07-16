using CubeWorld.Player.CharacterModel.Core;
using CubeWorld.Player.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Parts
{
    /// <summary>
    /// Tête chibi façon Link : boîte rectangulaire à coins adoucis (sections "squircle"
    /// empilées, largeur par rangée donnée par <see cref="HeadShape"/>), visage peint sur
    /// les voxels frontaux (yeux sclère+iris+éclat, bouche, blush, sourcils selon
    /// l'expression), OREILLES POINTUES d'elfe, bande de cheveux blonds (frange + mèches
    /// qui encadrent le visage + nuque) et BONNET vert pointu incliné vers l'arrière.
    /// Pivot à la base du cou : la tête s'anime en la faisant pivoter là.
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : variantes de bonnet dans <see cref="HairStyle"/>
    /// + le switch de StampCap ; galbe dans HeadShape.Generate ; taille des oreilles dans
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
            int capRows,
            float unit,
            int colorSeed
        )
        {
            int gridSize = body.HeadWidth + (HairMargin * 2);
            int rows = shape.RowWidth.Length;
            int gridHeight = NeckRows + rows + capRows + 1;

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
            StampSkullAndFace(grid, shape, palette, face, center, gridSize);
            StampEars(grid, shape, palette, center);
            StampHairAndCap(grid, body, shape, palette, center, gridSize, capRows);

            if (!face.EyesClosed)
            {
                PaintEyeSparkles(grid, shape, palette, center);
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

            if (row == shape.EyeRow)
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

            // Sourcils TOUJOURS peints (pas seulement pour les expressions marquées) : sans
            // eux, le regard perd son contraste et le visage lit "flou" à distance.
            int eyebrowRow =
                shape.EyeRow + Mathf.Max(2, Mathf.RoundToInt(shape.RowWidth.Length * 0.12f));
            if (row == eyebrowRow && eyebrowRow < shape.HairRowStart)
            {
                float browCenter = width * 0.20f;
                float browHalf = Mathf.Max(0.6f, width * 0.09f);
                if (dist >= browCenter - browHalf && dist <= browCenter + browHalf)
                {
                    return palette.HairShadow;
                }
            }

            if (row == shape.BlushRow)
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

        /// <summary>Bande de cheveux, frange, mèches latérales, puis le bonnet par-dessus.</summary>
        private static void StampHairAndCap(
            VoxelGrid grid,
            CharacterBodyParams body,
            HeadShape shape,
            CharacterPalette palette,
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

            // Bonnet : bord snug qui recouvre le haut du crâne (déborde d'un demi-voxel sur
            // la bande de cheveux), puis cône qui se resserre en s'inclinant vers l'arrière.
            bool tallCap = body.Hair == HairStyle.TallCap;
            float droop = tallCap ? 0.2f : 0.5f;
            int skullTopRow = shape.RowWidth.Length - 1;

            for (int row = shape.HairRowStart; row <= skullTopRow; row++)
            {
                float radius = Mathf.Max(1f, shape.RowWidth[row] / 2f) + 0.6f;
                FillCapRow(grid, row, radius, center, 0f, gridSize, palette.OutfitPrimary);
            }

            float baseRadius = Mathf.Max(1f, shape.RowWidth[skullTopRow] / 2f) + 0.6f;

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

        /// <summary>Ajuste la parité d'une largeur pour qu'elle se centre exactement dans la grille.</summary>
        private static int MatchParity(int gridSize, int width) =>
            (gridSize - width) % 2 == 0 ? width : width + 1;
    }
}
