using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWorld.CharacterModel.Core
{
    /// <summary>
    /// Convertit une <see cref="VoxelGrid"/> complète en Mesh : seules les faces exposées au
    /// vide (ou au bord de la grille) sont émises. Les vertices sont exprimés dans le repère
    /// LOCAL de la pièce : le pivot voxel passé en paramètre devient l'origine du mesh — c'est
    /// ce qui permet d'animer la pièce par simple rotation de son Transform (épaule, hanche,
    /// base du cou...), sans skinning.
    ///
    /// Le relief directionnel (faces éclairées/à l'ombre) est le travail du shader
    /// CubeWorld/VoxelCharacter ; le mesher bake ce que le shader ne peut pas savoir :
    /// - un léger résidu de shading par face (utile quand la lumière est de face) ;
    /// - le jitter de couleur par voxel (casse les aplats, signature CubeWorld) ;
    /// - l'OCCLUSION AMBIANTE voxel par sommet (creuse jointures et recoins), stockée dans
    ///   l'ALPHA des couleurs de vertex, avec flip de diagonale des quads pour éviter les
    ///   artefacts d'interpolation.
    /// </summary>
    internal static class VoxelGridMesher
    {
        // Résidu de shading par face (dessus à peine plus clair, dessous à peine plus
        // sombre). Volontairement discret : le vrai contraste directionnel vient des bandes
        // cel du shader — un résidu fort ferait double comptage et écraserait tout.
        private static readonly float[] FaceShade = { 0.96f, 1.0f, 1.06f, 0.88f, 0.94f, 0.94f };

        // Alpha vertex par niveau d'occlusion (0 = coin dégagé, 3 = coin enfermé).
        private static readonly byte[] AoAlpha = { 255, 217, 179, 128 };

        public static Mesh BuildMesh(
            VoxelGrid grid,
            string meshName,
            float unit,
            Vector3 pivotVoxels,
            int colorSeed
        )
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();

            for (int x = 0; x < grid.SizeX; x++)
            {
                for (int y = 0; y < grid.SizeY; y++)
                {
                    for (int z = 0; z < grid.SizeZ; z++)
                    {
                        if (!grid.IsSolid(x, y, z))
                        {
                            continue;
                        }

                        Color32 color = Jitter(grid.Get(x, y, z), colorSeed, x, y, z);

                        for (int face = 0; face < 6; face++)
                        {
                            Vector3Int dir = FaceDirection(face);

                            if (grid.IsSolid(x + dir.x, y + dir.y, z + dir.z))
                            {
                                continue;
                            }

                            AddFace(
                                grid,
                                vertices,
                                normals,
                                colors,
                                triangles,
                                new Vector3Int(x, y, z),
                                face,
                                color,
                                unit,
                                pivotVoxels
                            );
                        }
                    }
                }
            }

            var mesh = new Mesh { name = meshName };

            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            VoxelGrid grid,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color32> colors,
            List<int> triangles,
            Vector3Int cell,
            int face,
            Color32 color,
            float unit,
            Vector3 pivotVoxels
        )
        {
            Vector3 normal = FaceDirection(face);
            int baseIndex = vertices.Count;
            Color32 shadedColor = ApplyShade(color, FaceShade[face]);

            var ao = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = FaceCorner(face, i);
                ao[i] = CornerAo(grid, cell, face, corner);

                vertices.Add((new Vector3(cell.x, cell.y, cell.z) + corner - pivotVoxels) * unit);
                normals.Add(normal);
                colors.Add(new Color32(shadedColor.r, shadedColor.g, shadedColor.b, ao[i]));
            }

            // Flip de diagonale : l'interpolation de l'AO sur un quad dépend de la diagonale
            // choisie ; on prend celle qui relie les coins les plus occlus pour éviter les
            // "croix" claires dans les angles (technique voxel AO standard).
            if (ao[0] + ao[3] > ao[1] + ao[2])
            {
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }
            else
            {
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
            }
        }

        /// <summary>
        /// Occlusion d'un coin de face : on regarde, dans le plan juste devant la face, les
        /// deux voxels adjacents au coin (côtés) et le voxel diagonal. Deux côtés pleins =
        /// coin enfermé (niveau max), sinon on compte côtés + diagonale.
        /// </summary>
        private static byte CornerAo(VoxelGrid grid, Vector3Int cell, int face, Vector3 corner)
        {
            Vector3Int dir = FaceDirection(face);
            Vector3Int neighbor = cell + dir;

            // Axes tangents à la face + signe du coin sur chacun (corner vaut 0 ou 1).
            int axis = dir.x != 0 ? 0 : (dir.y != 0 ? 1 : 2);
            int axisU = axis == 0 ? 1 : 0;
            int axisV = axis == 2 ? 1 : 2;

            Vector3Int offsetU = AxisOffset(axisU, corner[axisU] > 0.5f ? 1 : -1);
            Vector3Int offsetV = AxisOffset(axisV, corner[axisV] > 0.5f ? 1 : -1);

            bool side1 = IsSolid(grid, neighbor + offsetU);
            bool side2 = IsSolid(grid, neighbor + offsetV);
            bool diagonal = IsSolid(grid, neighbor + offsetU + offsetV);

            int occlusion =
                side1 && side2 ? 3 : (side1 ? 1 : 0) + (side2 ? 1 : 0) + (diagonal ? 1 : 0);

            return AoAlpha[occlusion];
        }

        private static bool IsSolid(VoxelGrid grid, Vector3Int p) => grid.IsSolid(p.x, p.y, p.z);

        private static Vector3Int AxisOffset(int axis, int sign) =>
            axis switch
            {
                0 => new Vector3Int(sign, 0, 0),
                1 => new Vector3Int(0, sign, 0),
                _ => new Vector3Int(0, 0, sign),
            };

        private static Color32 ApplyShade(Color32 color, float factor) =>
            new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
                color.a
            );

        // Micro-variation de teinte par voxel : casse les grands aplats sans texture,
        // signature visuelle du style CubeWorld.
        private static Color32 Jitter(Color32 color, int seed, int x, int y, int z)
        {
            const float strength = 0.02f;
            float t = (HashToUnit(seed, x, y, z) * 2f) - 1f;
            float factor = 1f + (t * strength);

            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
                color.a
            );
        }

        private static float HashToUnit(int seed, int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)seed * 374761393u;
                h ^= (uint)x * 668265263u;
                h ^= (uint)y * 2246822519u;
                h ^= (uint)z * 3266489917u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0x00FFFFFFu) / (float)0x01000000u;
            }
        }

        private static Vector3Int FaceDirection(int face) =>
            face switch
            {
                0 => new Vector3Int(0, 0, -1),
                1 => new Vector3Int(0, 0, 1),
                2 => new Vector3Int(0, 1, 0),
                3 => new Vector3Int(0, -1, 0),
                4 => new Vector3Int(-1, 0, 0),
                _ => new Vector3Int(1, 0, 0),
            };

        private static Vector3 FaceCorner(int face, int i) =>
            face switch
            {
                0 => i switch
                {
                    0 => Corner(0),
                    1 => Corner(3),
                    2 => Corner(1),
                    _ => Corner(2),
                },
                1 => i switch
                {
                    0 => Corner(5),
                    1 => Corner(6),
                    2 => Corner(4),
                    _ => Corner(7),
                },
                2 => i switch
                {
                    0 => Corner(3),
                    1 => Corner(7),
                    2 => Corner(2),
                    _ => Corner(6),
                },
                3 => i switch
                {
                    0 => Corner(1),
                    1 => Corner(5),
                    2 => Corner(0),
                    _ => Corner(4),
                },
                4 => i switch
                {
                    0 => Corner(4),
                    1 => Corner(7),
                    2 => Corner(0),
                    _ => Corner(3),
                },
                _ => i switch
                {
                    0 => Corner(1),
                    1 => Corner(2),
                    2 => Corner(5),
                    _ => Corner(6),
                },
            };

        private static Vector3 Corner(int index) =>
            index switch
            {
                0 => new Vector3(0f, 0f, 0f),
                1 => new Vector3(1f, 0f, 0f),
                2 => new Vector3(1f, 1f, 0f),
                3 => new Vector3(0f, 1f, 0f),
                4 => new Vector3(0f, 0f, 1f),
                5 => new Vector3(1f, 0f, 1f),
                6 => new Vector3(1f, 1f, 1f),
                _ => new Vector3(0f, 1f, 1f),
            };
    }
}
