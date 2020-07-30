using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{



    public class HeightMap
    {
        public float[,] Heights;
        public Vector3[,] LocalVertexPositions;
        public TerrainSettings TerrainSettings;

        public HeightMap(float[,] heights, Vector3[,] vertices, TerrainSettings terrainSettings)
        {
            Heights = heights;
            LocalVertexPositions = vertices;
            TerrainSettings = terrainSettings;

            //DebugMinMax();
        }


        public void DebugMinMax()
        {
            if (Heights != null)
            {
                int width = Heights.GetLength(0), height = Heights.GetLength(1);
                float min = Heights[0, 0], max = Heights[0, 0];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float curr = Heights[x, y];
                        if (curr < min)
                        {
                            min = curr;
                        }
                        if (curr > max)
                        {
                            max = curr;
                        }
                    }
                }

                Debug.Log("min: " + min + " max: " + max);
            }
        }
    }
}
