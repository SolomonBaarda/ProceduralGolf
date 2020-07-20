using System.Collections.Generic;
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


        public Mesh GenerateMesh(MeshSettings settings)
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
        [Header("1 (full), 2 (half), 4 (quarter), etc...")]
        [Min(1)]
        public int LevelOfDetail = 1;

        public override void ValidateValues()
        {
            LevelOfDetail = Mathf.ClosestPowerOfTwo(Mathf.Max(LevelOfDetail, 1));
        }


    }


}
