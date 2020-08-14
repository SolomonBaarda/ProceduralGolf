using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{


    public static MeshData GenerateMeshData(TerrainMap terrainMap)
    {
        MeshData data = new MeshData(terrainMap.Width, terrainMap.Height);

        // Loop through each vertex
        for (int y = 0; y < terrainMap.Height; y++)
        {
            for (int x = 0; x < terrainMap.Width; x++)
            {
                // Set the vertex depending on the biome
                MeshData.BiomeData b = data.GetBiomeData(terrainMap.Map[x, y].Biome);

                b.SetVertex(x, y, terrainMap.Map[x, y].LocalVertexPosition);
            }
        }

        // Recalculate all the UVs
        foreach(MeshData.BiomeData b in data.Biomes.Values)
        {
            b.CalculateUVS();
        }

        return data;
    }





    public class MeshData
    {
        private readonly int Width, Height;

        public Dictionary<TerrainSettings.Biome, BiomeData> Biomes = new Dictionary<TerrainSettings.Biome, BiomeData>();
        public Dictionary<TerrainSettings.Biome, LevelOfDetail> Meshes = new Dictionary<TerrainSettings.Biome, LevelOfDetail>();


        public MeshData(int verticesX, int verticesY)
        {
            Width = verticesX; Height = verticesY;
        }


        public BiomeData GetBiomeData(TerrainSettings.Biome biome)
        {
            if (!Biomes.TryGetValue(biome, out BiomeData data))
            {
                data = new BiomeData(Width, Height, biome);
                Biomes.Add(biome, data);
            }

            return data;
        }


        public void RecalculateAllLODs(MeshSettings settings)
        {
            Meshes.Clear();

            // Calculate the LODs for each biome
            foreach(BiomeData biome in Biomes.Values)
            {
                Meshes.Add(biome.Biome, biome.GenerateLOD(settings));
            }
        }


        public class BiomeData
        {
            private readonly int Width, Height;
            public TerrainSettings.Biome Biome;
            public Vector3[] Vertices;
            public Vector2[] UVs;

            public BiomeData(int width, int height, TerrainSettings.Biome biome)
            {
                Width = width;
                Height = height;
                Biome = biome;

                // Assign array size
                Vertices = new Vector3[Width * Height];
                UVs = new Vector2[Vertices.Length];
            }




            private int GetVertexIndex(int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    return y * Width + x;
                }
                return -1;
            }

            public void SetVertex(int x, int y, Vector3 vertex)
            {
                int index = GetVertexIndex(x, y);
                if (index != -1)
                {
                    Vertices[index] = vertex;
                }
            }


            public void CalculateUVS()
            {
                UVs = new Vector2[Vertices.Length];

                // Get the minimum and maximum points
                Vector2 min = Vertices[0], max = Vertices[0];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector3 v = Vertices[GetVertexIndex(x, y)];

                        if (v.x < min.x) { min.x = v.x; }
                        if (v.x > max.x) { max.x = v.x; }

                        if (v.z < min.y) { min.y = v.z; }
                        if (v.z > max.y) { max.y = v.z; }
                    }
                }

                Vector2 size = max - min;

                // Now assign each UV
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector3 point = Vertices[GetVertexIndex(x, y)];

                        UVs[GetVertexIndex(x, y)] = (max - new Vector2(point.x, point.z)) / size;
                    }
                }
            }


            public LevelOfDetail GenerateLOD(MeshSettings settings)
            {
                int i = settings.SimplificationIncrement;

                int newWidth = (Width - 1) / i + 1, newHeight = (Height - 1) / i + 1;

                Vector3[] newVertices = new Vector3[newWidth * newHeight];
                Vector2[] newUVs = new Vector2[newVertices.Length];
                int[] newTriangles = new int[newWidth * newHeight * 6];
                int triangleIndex = 0;

                // Add all the correct vertices
                for (int y = 0; y < Height; y += i)
                {
                    for (int x = 0; x < Width; x += i)
                    {
                        int newX = x / i, newY = y / i;
                        int thisVertexIndex = newY * newWidth + newX;
                        // Add the vertex
                        newVertices[thisVertexIndex] = Vertices[GetVertexIndex(x, y)];
                        // Add the UV
                        newUVs[thisVertexIndex] = UVs[GetVertexIndex(x, y)];

                        // Set the triangles
                        if (newX >= 0 && newX < newWidth - 1 && newY >= 0 && newY < newHeight - 1)
                        {
                            newTriangles[triangleIndex] = thisVertexIndex;
                            // Below
                            newTriangles[triangleIndex + 1] = thisVertexIndex + newWidth;
                            // Bottom right
                            newTriangles[triangleIndex + 2] = thisVertexIndex + newWidth + 1;
                            triangleIndex += 3;

                            newTriangles[triangleIndex] = thisVertexIndex;
                            // Bottom right
                            newTriangles[triangleIndex + 1] = thisVertexIndex + newWidth + 1;
                            // Top right
                            newTriangles[triangleIndex + 2] = thisVertexIndex + 1;
                            triangleIndex += 3;
                        }
                    }
                }

                return new LevelOfDetail(newWidth, newHeight, newVertices, newUVs, newTriangles);
            }

        }
    }



    public class LevelOfDetail
    {
        private readonly int Width, Height;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public int[] Triangles;

        public LevelOfDetail(int width, int height, Vector3[] vertices, Vector2[] uvs, int[] triangles)
        {
            Width = width;
            Height = height;

            Vertices = vertices;
            UVs = uvs;
            Triangles = triangles;
        }

        public Mesh GenerateMesh()
        {
            Mesh m = new Mesh()
            {
                vertices = Vertices,
                triangles = Triangles,
                uv = UVs,
            };

            m.RecalculateNormals();
            m.Optimize();

            return m;
        }

    }

}
