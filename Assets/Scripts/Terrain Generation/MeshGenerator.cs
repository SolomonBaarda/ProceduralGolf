﻿using UnityEngine;

public static class MeshGenerator
{
    public static void OptimiseMesh(ref Mesh m)
    {
        // Recalculate values
        m.RecalculateNormals();
        m.Optimize();
    }

    public static void UpdateMeshData(ref MeshData data, TerrainMap terrainMap, Vector3[] baseVertices)
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
                int index = y * terrainMap.Width + x;

                data.Vertices[index] = baseVertices[index];
                data.Vertices[index].y += terrainMap.Heights[index];
            }
        }

        data.UpdateUVS();
    }



    public class MeshData
    {
        public int Width, Height;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        //public Color[] Colours;


        public MeshData(int width, int height)
        {
            Width = width; Height = height;

            // Assign memory for the arrays
            Vertices = new Vector3[Width * Height];
            UVs = new Vector2[Vertices.Length];
            //Colours = new Color[Vertices.Length];
        }


        public void UpdateUVS()
        {
            // Get the minimum and maximum points
            Vector2 min = Vertices[0], max = Vertices[0];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector3 v = Vertices[y * Width + x];

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
                    Vector3 point = Vertices[y * Width + x];

                    UVs[y * Width + x] = (max - new Vector2(point.x, point.z)) / size;
                }
            }
        }



        public void UpdateMesh(ref Mesh m, in MeshSettings settings)
        {
            int i = settings.SimplificationIncrement;
            int newWidth = (Width - 1) / i + 1, newHeight = (Height - 1) / i + 1;

            Vector3[] newVertices = new Vector3[newWidth * newHeight];
            Vector2[] newUVs = new Vector2[newVertices.Length];
            //Color[] newColours = new Color[newVertices.Length];
            int[] newTriangles = new int[newVertices.Length * 6];

            int triangleIndex = 0;
            // Add all the correct vertices
            for (int y = 0; y < Height; y += i)
            {
                for (int x = 0; x < Width; x += i)
                {
                    int newX = x / i, newY = y / i;
                    int thisVertexIndex = newY * newWidth + newX;
                    int oldIndex = y * Width + x;
                    // Add the vertex
                    newVertices[thisVertexIndex] = Vertices[oldIndex];
                    // Add the UV
                    newUVs[thisVertexIndex] = UVs[oldIndex];
                    // Add the colour
                    //newColours[thisVertexIndex] = Colours[GetVertexIndex(x, y)];

                    if (newX >= 0 && newX < newWidth - 1 && newY >= 0 && newY < newHeight - 1)
                    {
                        // Set the triangles
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
            //m.colors = newColours;
        }



    }



}
