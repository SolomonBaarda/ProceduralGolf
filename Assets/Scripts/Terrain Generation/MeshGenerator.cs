using System;
using UnityEngine;

public static class MeshGenerator
{

    public static MeshData GenerateMeshData(TerrainMapGenerator.TerrainMap terrainMap)
    {
        MeshData data = new MeshData(terrainMap.Width, terrainMap.Height);

        // Loop through each vertex
        for (int y = 0; y < terrainMap.Height; y++)
        {
            for (int x = 0; x < terrainMap.Width; x++)
            {
                data.SetVertex(x, y, terrainMap.Map[x,y].LocalVertexPosition);
            }
        }

        return data;
    }





    public class MeshData
    {
        private readonly int MaxVerticesWidth, MaxVerticesHeight;
        public Vector3[] Vertices;
        public Vector2[] UVs;


        public MeshData(int verticesX, int verticesY)
        {
            MaxVerticesWidth = verticesX; MaxVerticesHeight = verticesY;

            // Assign array size
            Vertices = new Vector3[MaxVerticesWidth * MaxVerticesHeight];
        }



        private int GetVertexIndex(int x, int y)
        {
            if (x >= 0 && x < MaxVerticesWidth && y >= 0 && y < MaxVerticesHeight)
            {
                return y * MaxVerticesWidth + x;
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
            for (int y = 0; y < MaxVerticesHeight; y++)
            {
                for (int x = 0; x < MaxVerticesWidth; x++)
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
            for (int y = 0; y < MaxVerticesHeight; y++)
            {
                for (int x = 0; x < MaxVerticesWidth; x++)
                {
                    Vector3 point = Vertices[GetVertexIndex(x, y)];

                    UVs[GetVertexIndex(x, y)] = (max - new Vector2(point.x, point.z)) / size;
                }
            }

        }


        public Mesh GenerateMesh(MeshSettings settings)
        {
            CalculateUVS();

            int i = settings.SimplificationIncrement;

            int newWidthVertices = (MaxVerticesWidth - 1) / i + 1, newHeightVertices = (MaxVerticesHeight - 1) / i + 1;

            Vector3[] vertices = new Vector3[newWidthVertices * newHeightVertices];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[newWidthVertices * newHeightVertices * 6];
            int triangleIndex = 0;

            // Add all the correct vertices
            for (int y = 0; y < MaxVerticesHeight; y += i)
            {
                for (int x = 0; x < MaxVerticesWidth; x += i)
                {
                    int newX = x / i, newY = y / i;
                    int thisVertexIndex = newY * newWidthVertices + newX;
                    // Add the vertex
                    vertices[thisVertexIndex] = Vertices[GetVertexIndex(x, y)];
                    // Add the UV
                    uvs[thisVertexIndex] = UVs[GetVertexIndex(x, y)];

                    // Set the triangles
                    if (newX >= 0 && newX < newWidthVertices - 1 && newY >= 0 && newY < newHeightVertices - 1)
                    {
                        triangles[triangleIndex] = thisVertexIndex;
                        // Below
                        triangles[triangleIndex + 1] = thisVertexIndex + newWidthVertices;
                        // Bottom right
                        triangles[triangleIndex + 2] = thisVertexIndex + newWidthVertices + 1;
                        triangleIndex += 3;

                        triangles[triangleIndex] = thisVertexIndex;
                        // Bottom right
                        triangles[triangleIndex + 1] = thisVertexIndex + newWidthVertices + 1;
                        // Top right
                        triangles[triangleIndex + 2] = thisVertexIndex + 1;
                        triangleIndex += 3;
                    }
                }
            }

            Mesh m = new Mesh()
            {
                vertices = vertices,
                triangles = triangles,
                uv = uvs,
            };

            m.RecalculateNormals();
            m.Optimize();

            return m;
        }


    }





}
