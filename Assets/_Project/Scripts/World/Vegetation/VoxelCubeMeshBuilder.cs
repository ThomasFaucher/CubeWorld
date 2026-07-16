using System;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Primitives de mesh voxel partagées par les arbres et les nuages (mêmes
    /// volumes de mini-cubes que <c>Player.CharacterModel.Core.VoxelStamper</c>,
    /// copie indépendante côté World — voir docs/ARCHITECTURE.md, World ne doit
    /// jamais dépendre de Player).
    /// </summary>
    internal static class VoxelCubeMeshBuilder
    {
        internal static void AddPart(
            VoxelCubeMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            VoxelCubeShape shape,
            Color32 color,
            float unit,
            int seed
        )
        {
            AddPart(mesh, origin, sx, sy, sz, shape, (_, _, _) => color, unit, seed);
        }

        internal static void AddPart(
            VoxelCubeMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            VoxelCubeShape shape,
            Func<int, int, int, Color32> colorAt,
            float unit,
            int seed
        )
        {
            _ = seed; // conservé pour l'API (variantes / appelants existants)
            bool[,,] mask = BuildMask(shape, sx, sy, sz);
            AddMasked(mesh, origin, sx, sy, sz, mask, colorAt, unit);
        }

        /// <summary>
        /// Volume libre : <paramref name="isSolid"/> décide cube par cube
        /// (troncs coniques, houppiers irréguliers…).
        /// </summary>
        internal static void AddCustom(
            VoxelCubeMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            Func<int, int, int, bool> isSolid,
            Func<int, int, int, Color32> colorAt,
            float unit
        )
        {
            var mask = new bool[sx, sy, sz];
            for (int x = 0; x < sx; x++)
            {
                for (int y = 0; y < sy; y++)
                {
                    for (int z = 0; z < sz; z++)
                    {
                        mask[x, y, z] = isSolid(x, y, z);
                    }
                }
            }

            AddMasked(mesh, origin, sx, sy, sz, mask, colorAt, unit);
        }

        private static void AddMasked(
            VoxelCubeMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            bool[,,] mask,
            Func<int, int, int, Color32> colorAt,
            float unit
        )
        {
            for (int x = 0; x < sx; x++)
            {
                for (int y = 0; y < sy; y++)
                {
                    for (int z = 0; z < sz; z++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        Color32 color = colorAt(x, y, z);

                        for (int face = 0; face < 6; face++)
                        {
                            Vector3Int dir = FaceDirection(face);
                            int nx = x + dir.x;
                            int ny = y + dir.y;
                            int nz = z + dir.z;

                            bool neighborSolid =
                                nx >= 0
                                && nx < sx
                                && ny >= 0
                                && ny < sy
                                && nz >= 0
                                && nz < sz
                                && mask[nx, ny, nz];

                            if (!neighborSolid)
                            {
                                AddFace(mesh, origin, x, y, z, face, color, unit);
                            }
                        }
                    }
                }
            }
        }

        // Volume plein (Box), colonne à section arrondie (Column, coins coupés
        // sur X/Z, pleine hauteur sur Y) ou sphère voxelisée (Sphere, coins
        // coupés sur les 3 axes — un nuage aplati n'est qu'une Sphere avec
        // sy < sx/sz).
        private static bool[,,] BuildMask(VoxelCubeShape shape, int sx, int sy, int sz)
        {
            var mask = new bool[sx, sy, sz];
            float cx = (sx - 1) / 2f;
            float cy = (sy - 1) / 2f;
            float cz = (sz - 1) / 2f;
            float rx = sx / 2f;
            float ry = sy / 2f;
            float rz = sz / 2f;

            for (int x = 0; x < sx; x++)
            {
                for (int y = 0; y < sy; y++)
                {
                    for (int z = 0; z < sz; z++)
                    {
                        mask[x, y, z] = shape switch
                        {
                            VoxelCubeShape.Box => true,
                            VoxelCubeShape.Column => Sq((x - cx) / rx) + Sq((z - cz) / rz) <= 0.82f,
                            VoxelCubeShape.Sphere => Sq((x - cx) / rx)
                                + Sq((y - cy) / ry)
                                + Sq((z - cz) / rz)
                                <= 0.92f,
                            _ => true,
                        };
                    }
                }
            }

            return mask;
        }

        private static float Sq(float v) => v * v;

        private static void AddFace(
            VoxelCubeMeshData mesh,
            Vector3Int origin,
            int x,
            int y,
            int z,
            int face,
            Color32 color,
            float unit
        )
        {
            Vector3 normal = FaceDirection(face);
            int baseIndex = mesh.Vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                Vector3 corner =
                    new Vector3(origin.x + x, origin.y + y, origin.z + z) + FaceCorner(face, i);
                mesh.Vertices.Add(corner * unit);
                mesh.Normals.Add(normal);
                mesh.Colors.Add(color);
            }

            mesh.Triangles.Add(baseIndex + 0);
            mesh.Triangles.Add(baseIndex + 1);
            mesh.Triangles.Add(baseIndex + 2);
            mesh.Triangles.Add(baseIndex + 2);
            mesh.Triangles.Add(baseIndex + 1);
            mesh.Triangles.Add(baseIndex + 3);
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
