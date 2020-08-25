using UnityEngine;

public static class MeshGenerator
{


    public static void UpdateMeshData(ref MeshData data, TerrainMap terrainMap)
    {
        // Create the data if we need to 
        if (data == null)
        {
            data = new MeshData(terrainMap.Width, terrainMap.Height);
        }

        // Loop through each vertex
        for (int y = 0; y < terrainMap.Height; y++)
        {
            for (int x = 0; x < terrainMap.Width; x++)
            {
                data.SetVertex(x, y, terrainMap.Map[x, y].LocalVertexPosition, Biome.BiomeToColour(terrainMap.Map[x, y].Biome));
            }
        }

        data.UpdateUVS();
    }



    public class MeshData
    {
        private readonly int MaxVerticesWidth, MaxVerticesHeight;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public Color[] Colours;


        public MeshData(int verticesX, int verticesY)
        {
            MaxVerticesWidth = verticesX; MaxVerticesHeight = verticesY;

            // Assign memory for the arrays
            Vertices = new Vector3[MaxVerticesWidth * MaxVerticesHeight];
            UVs = new Vector2[Vertices.Length];
            Colours = new Color[Vertices.Length];
        }



        private int GetVertexIndex(int x, int y)
        {
            if (x >= 0 && x < MaxVerticesWidth && y >= 0 && y < MaxVerticesHeight)
            {
                return y * MaxVerticesWidth + x;
            }
            return -1;
        }

        public void SetVertex(int x, int y, Vector3 vertex, Color colour)
        {
            int index = GetVertexIndex(x, y);
            if (index != -1)
            {
                Vertices[index] = vertex;
                Colours[index] = colour;
            }
        }


        public void UpdateUVS()
        {
            // Get the minimum and maximum points
            Vector2 min = Vertices[0], max = Vertices[0];
            for (int y = 0; y < MaxVerticesHeight; y++)
            {
                for (int x = 0; x < MaxVerticesWidth; x++)
                {
                    Vector3 v = Vertices[y * MaxVerticesWidth + x];

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
                    Vector3 point = Vertices[y * MaxVerticesWidth + x];

                    UVs[y * MaxVerticesWidth + x] = (max - new Vector2(point.x, point.z)) / size;
                }
            }
        }



        public void UpdateMesh(ref Mesh m, in MeshSettings settings)
        {
            int i = settings.SimplificationIncrement;

            int newWidth = (MaxVerticesWidth - 1) / i + 1, newHeight = (MaxVerticesHeight - 1) / i + 1;

            Vector3[] newVertices = new Vector3[newWidth * newHeight];
            Vector2[] newUVs = new Vector2[newVertices.Length];
            Color[] newColours = new Color[newVertices.Length];
            int[] newTriangles = new int[newVertices.Length * 6];
            int triangleIndex = 0;

            // Add all the correct vertices
            for (int y = 0; y < MaxVerticesHeight; y += i)
            {
                for (int x = 0; x < MaxVerticesWidth; x += i)
                {
                    int newX = x / i, newY = y / i;
                    int thisVertexIndex = newY * newWidth + newX;
                    // Add the vertex
                    newVertices[thisVertexIndex] = Vertices[GetVertexIndex(x, y)];
                    // Add the UV
                    newUVs[thisVertexIndex] = UVs[GetVertexIndex(x, y)];
                    // Add the colour
                    newColours[thisVertexIndex] = Colours[GetVertexIndex(x, y)];


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




            // Create the new mesh if we need to
            if (m == null)
            {
                m = new Mesh();
            }

            // Overwrite the current mesh values to prevent extra memory being allocated then removed
            m.vertices = newVertices;
            m.triangles = newTriangles;
            m.uv = newUVs;
            m.colors = newColours;

            // Recalculate values
            m.RecalculateNormals();
            m.Optimize();
        }

    }



}
