using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public static class MeshGenerator
{

    public static MeshData GenerateMeshData(TerrainGenerator.HeightMap heightMap, MeshSettings settings, Bounds chunkBounds)
    {
        int width = heightMap.Heights.GetLength(0), height = heightMap.Heights.GetLength(1);
        MeshData data = new MeshData(width, height);

        Vector3 distanceBetweenVertices = CalculateDistanceBetweenVertices(chunkBounds, settings.TotalVertices);
        int firstTraingleIndex = 0;

        // Loop through each vertex
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float vertexheight = heightMap.Heights[x, y] * heightMap.Scale;

                // Calculate the point of the vertex
                Vector3 point = new Vector3(distanceBetweenVertices.x * x, vertexheight, distanceBetweenVertices.y * y);
                data.SetVertex(x, y, point);

                if (x >= 0 && x < width - 1 && y >= 0 && y < height - 1)
                {
                    // Set the two triangles
                    data.SetTriangle(firstTraingleIndex, x, y, x + 1, y + 1, x, y + 1);
                    firstTraingleIndex += 3;
                    data.SetTriangle(firstTraingleIndex, x, y, x + 1, y, x + 1, y + 1);
                    firstTraingleIndex += 3;
                }
            }
        }



        return data;
    }


    public static Vector3 CalculateDistanceBetweenVertices(Bounds b, int totalVertices)
    {
        return (b.max - b.min) / totalVertices;
    }

    public static Vector2[,] CalculatePointsToSampleFrom(Bounds chunk, MeshSettings settings)
    {
        settings.ValidateValues();

        Vector2[,] pointsToSample = new Vector2[settings.TotalVertices, settings.TotalVertices];
        Vector2 distanceBetweenVertices = CalculateDistanceBetweenVertices(chunk, settings.TotalVertices);

        // Iterate over each point
        for (int y = 0; y < settings.TotalVertices; y++)
        {
            for (int x = 0; x < settings.TotalVertices; x++)
            {
                // Calculate the point in space to sample from
                pointsToSample[x, y] = new Vector2(chunk.min.x, chunk.min.z) + (new Vector2(x, y) * distanceBetweenVertices);
            }
        }

        return pointsToSample;
    }



    public class MeshData
    {
        private readonly int width, height;
        public Vector3[] Vertices;
        public int[] triangles;


        public MeshData(int verticesX, int verticesY, int facesPervertex = 2)
        {
            width = verticesX; height = verticesY;

            // Assign array size
            Vertices = new Vector3[width * height];
            triangles = new int[Vertices.Length * 3 * facesPervertex];
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

        public void SetTriangle(int firstIndex, int aX, int aY, int bX, int bY, int cX, int cY)
        {
            triangles[firstIndex] = GetVertexIndex(aX, aY);
            triangles[firstIndex + 1] = GetVertexIndex(bX, bY);
            triangles[firstIndex + 2] = GetVertexIndex(cX, cY);
        }


        public Mesh CreateMesh()
        {
            Mesh m = new Mesh()
            {
                vertices = Vertices,
                triangles = triangles
            };
            m.RecalculateNormals();
            //m.Optimize();

            return m;
        }
    }



    [System.Serializable]
    [CreateAssetMenu()]
    public class MeshSettings : ISettings
    {
        public int NoiseSamplePointDensity = 16;
        public int TotalVertices;

        public override void ValidateValues()
        {
            NoiseSamplePointDensity = Mathf.Max(NoiseSamplePointDensity, 1);
            // There is always one extra vertex than face
            TotalVertices = NoiseSamplePointDensity + 1;
        }

    }
}
