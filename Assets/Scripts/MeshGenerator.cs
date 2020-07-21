using UnityEngine;

public static class MeshGenerator
{

    public static MeshData GenerateMeshData(HeightMapGenerator.HeightMap heightMap)
    {
        int width = heightMap.Heights.GetLength(0), height = heightMap.Heights.GetLength(1);
        MeshData data = new MeshData(width, height);

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


        public Mesh GenerateMesh(MeshSettings settings)
        {
            int i = settings.SimplificationIncrement;

            int newWidthVertices = (MaxVerticesWidth - 1) / i + 1, newHeightVertices = (MaxVerticesHeight - 1) / i + 1;
            Vector3[] vertices = new Vector3[newWidthVertices * newHeightVertices];
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
            };
            m.RecalculateNormals();
            //m.Optimize();

            return m;
        }


    }


    [CreateAssetMenu()]
    public class MeshSettings : VariablePreset
    {
        [Header("Value of 1 (most detail) to 6 (least detail)")]
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
