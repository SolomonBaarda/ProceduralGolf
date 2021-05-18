using System.Collections.Generic;
using UnityEngine;

public class FloodFillBiome
{
    public Biome.Type Biome;
    public Vector3 MinMin, MinMax, MaxMin, MaxMax;

    public FloodFillBiome(Biome.Type biome, Vector3 initialWorldPos)
    {
        Biome = biome;
        MinMin = initialWorldPos;
        MinMax = initialWorldPos;
        MaxMin = initialWorldPos;
        MaxMax = initialWorldPos;
    }

    public void CheckPoint(Vector3 biome)
    {
        UpdateMin(biome.x, ref MinMin.x);
        UpdateMin(biome.y, ref MinMin.y);

        UpdateMin(biome.x, ref MinMax.x);
        UpdateMax(biome.y, ref MinMax.y);

        UpdateMax(biome.x, ref MaxMin.x);
        UpdateMin(biome.y, ref MaxMin.y);

        UpdateMax(biome.x, ref MaxMax.x);
        UpdateMax(biome.y, ref MaxMax.y);
    }

    private void UpdateMin(float value, ref float min)
    {
        if (value < min)
            min = value;
    }
    private void UpdateMax(float value, ref float max)
    {
        if (value > max)
            max = value;
    }

    /*
    public Vector3 CalculateCentre()
    {
        
        if (Vertices.Count > 0)
        {
            TerrainMap.Point random = Vertices.FirstOrDefault();
            Vector3 min = random.LocalVertexPosition + random.Offset, max = min;

            foreach (TerrainMap.Point p in Vertices)
            {
                Vector3 v = p.LocalVertexPosition + p.Offset;

                if (v.x < min.x) { min.x = v.x; }
                if (v.z < min.z) { min.z = v.z; }

                if (v.x > max.x) { max.x = v.x; }
                if (v.z > max.z) { max.z = v.z; }
            }

            Vector3 centreOffset = (max + min) / 2;

            return centreOffset;
        }
        else
        {
            return default;
        }
    }
    




    private float CalculateAverageHeight()
    {
        int totalVertices = 0;
        float totalHeight = 0;

        foreach (PointsForMap p in Points)
        {
            foreach ((int, int) pos in p.Indexes)
            {
                //totalHeight += p.Map.Heights[pos.Item1, pos.Item2];
                totalVertices++;
            }
        }


        // Get the average height for all points
        return totalHeight / totalVertices;
    }

    */




}
