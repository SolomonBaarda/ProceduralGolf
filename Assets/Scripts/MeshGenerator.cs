using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public static class MeshGenerator
{

    public static MeshData GenerateMeshData(TerrainGenerator.HeightMap heightMap, MeshSettings settings, Bounds chunkBounds)
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

                if (x >= 0 && x < width - 1 && y >= 0 && y < height - 1)
                {
                    // Set the two triangles
                    data.SetTriangle(x, y, x, y + 1, x + 1, y + 1);
                    data.SetTriangle(x, y, x + 1, y + 1, x + 1, y);
                }
            }
        }



        return data;
    }


    public static Vector3 CalculateDistanceBetweenVertices(Bounds b, int divisions)
    {
        return (b.max - b.min) / divisions;
    }





    public static Vector3[,] CalculateVertexPointsForChunk(in Bounds chunk, MeshSettings settings)
    {
        settings.ValidateValues();

        Vector3[,] roughVertices = new Vector3[settings.TotalVertices, settings.TotalVertices];
        Vector3 distanceBetweenVertices = CalculateDistanceBetweenVertices(chunk, settings.Faces);

        // Iterate over each point
        for (int y = 0; y < settings.TotalVertices; y++)
        {
            for (int x = 0; x < settings.TotalVertices; x++)
            {
                // Calculate the 3d point
                roughVertices[x, y] = chunk.min + new Vector3(x * distanceBetweenVertices.x, distanceBetweenVertices.y, y * distanceBetweenVertices.z);
            }
        }

        return roughVertices;
    }

    public static Vector3[,] CalculateLocalVertexPointsForChunk(in Vector3[,] worldPoints, in Vector3 centre)
    {
        int width = worldPoints.GetLength(0), height = worldPoints.GetLength(1);
        Vector3[,] localPositions = new Vector3[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                localPositions[x, y] = worldPoints[x, y] - centre;
            }
        }

        return localPositions;
    }


    public class MeshData
    {
        private readonly int width, height;
        public Vector3[] Vertices;
        public List<int> Triangles = new List<int>();


        public MeshData(int verticesX, int verticesY, int facesPervertex = 2)
        {
            width = verticesX; height = verticesY;

            // Assign array size
            Vertices = new Vector3[width * height];
            Triangles = new List<int>();
        }



        private int GetVertexIndex(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return y * width + x;
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

        public void SetTriangle(int aX, int aY, int bX, int bY, int cX, int cY)
        {
            Triangles.Add(GetVertexIndex(aX, aY));
            Triangles.Add(GetVertexIndex(bX, bY));
            Triangles.Add(GetVertexIndex(cX, cY));
        }


        public Mesh CreateMesh()
        {
            Mesh m = new Mesh()
            {
                vertices = Vertices,
                triangles = Triangles.ToArray(),
            };
            m.RecalculateNormals();
            //m.Optimize();

            return m;
        }
    }


    [CreateAssetMenu()]
    public class MeshSettings : VariablePreset
    {
        public int Faces = 8;
        [HideInInspector]
        public int TotalVertices;

        public override void ValidateValues()
        {
            Faces = Mathf.Max(Faces, 1);
            // There is always one extra vertex than face
            TotalVertices = Faces + 1;
        }

    }
}
