using System;
using UnityEngine;

namespace CubeWorld.Player.VoxelModels
{
    /// <summary>Primitives de mesh voxel partagées par tous les archétypes.</summary>
    internal static class PlayerVoxelMeshCore
    {
        internal const float HairPoofDomeCutoff = 0.30f;

        internal static void AddPart(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            PlayerVoxelShape shape,
            Color32 color,
            float unit,
            int seed
        )
        {
            AddPart(mesh, origin, sx, sy, sz, shape, (_, _, _) => color, unit, seed);
        }

        internal static void AddPart(
            PlayerVoxelMeshData mesh,
            Vector3Int origin,
            int sx,
            int sy,
            int sz,
            PlayerVoxelShape shape,
            Func<int, int, int, Color32> colorAt,
            float unit,
            int seed
        )
        {
            bool[,,] mask = BuildMask(shape, sx, sy, sz);

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

                        Color32 color = Jitter(
                            colorAt(x, y, z),
                            seed,
                            origin.x + x,
                            origin.y + y,
                            origin.z + z
                        );

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

        private static bool[,,] BuildMask(PlayerVoxelShape shape, int sx, int sy, int sz)
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
                        float sphereDistance =
                            Sq((x - cx) / rx) + Sq((y - cy) / ry) + Sq((z - cz) / rz);

                        mask[x, y, z] = shape switch
                        {
                            PlayerVoxelShape.Box => true,
                            PlayerVoxelShape.Column => Sq((x - cx) / rx) + Sq((z - cz) / rz) <= 0.82f,
                            PlayerVoxelShape.Sphere => sphereDistance <= 0.92f,
                            PlayerVoxelShape.Dome => y >= sy * HairPoofDomeCutoff && sphereDistance <= 0.92f,
                            _ => true,
                        };
                    }
                }
            }

            return mask;
        }

        private static float Sq(float v) => v * v;

        private static void AddFace(
            PlayerVoxelMeshData mesh,
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
