using System;
using UnityEngine;

public static class MeshGenerator
{

    public static MeshData GenerateMeshData(HeightMapGenerator.HeightMap heightMap, Vector3 meshCentreWorld)
    {
        int width = heightMap.Heights.GetLength(0), height = heightMap.Heights.GetLength(1);
        MeshData data = new MeshData(width, height, meshCentreWorld);

        // Loop through each vertex
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float vertexHeight;
                if (heightMap.TerrainSettings.UseCurve)
                {
                    vertexHeight = heightMap.TerrainSettings.HeightDistribution.Evaluate(heightMap.Heights[x, y]);
                }
                else
                {
                    vertexHeight = heightMap.Heights[x, y];
                }
                vertexHeight *= heightMap.TerrainSettings.HeightMultiplier;

                // Calculate the point of the vertex
                Vector3 point = heightMap.LocalVertexPositions[x, y] + new Vector3(0, -vertexHeight, 0);
                data.SetVertex(x, y, point);
            }
        }



        return data;
    }





    public class MeshData
    {
        private readonly int MaxVerticesWidth, MaxVerticesHeight;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        private Vector3 Min => Vertices[0];
        private Vector3 Max => Vertices[Vertices.Length - 1];


        public MeshData(int verticesX, int verticesY, Vector3 centreMeshWorld)
        {
            MaxVerticesWidth = verticesX; MaxVerticesHeight = verticesY;

            // Assign array size
            Vertices = new Vector3[MaxVerticesWidth * MaxVerticesHeight];
            UVs = new Vector2[Vertices.Length];
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

                float distanceX = Mathf.Abs(Max.x - vertex.x), distanceY = Mathf.Abs(Max.z - vertex.z);
                float maxDistanceX = Mathf.Abs(Max.x - Min.x), maxDistanceY = Mathf.Abs(Max.z - Min.z);

                UVs[index] = new Vector2(distanceX / maxDistanceX, distanceY / maxDistanceY);
            }
        }


        public Mesh GenerateMesh(MeshSettings settings)
        {
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


    [CreateAssetMenu()]
    public class MeshSettings : VariablePreset
    {
        [Header("Value of 0 (most detail) to 6 (least detail)")]
        public int LevelOfDetail = 1;
        public int SimplificationIncrement
        {
            get
            {
                ValidateValues();
                return Mathf.Max(LevelOfDetail * 2, 1);
            }
        }

        public override void ValidateValues()
        {
            LevelOfDetail = Mathf.Clamp(LevelOfDetail, 0, 6);
        }


    }


}
