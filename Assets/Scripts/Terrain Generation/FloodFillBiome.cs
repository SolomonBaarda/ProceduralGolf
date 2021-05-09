using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloodFillBiome
{
    public Biome.Type Biome;

    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;

    public class PointsForMap
    {
        public TerrainMap Map;
        public List<(int, int)> Indexes;
    }

    public List<PointsForMap> Points = new List<PointsForMap>();

    public FloodFillBiome(Biome.Type biome)
    {
        Biome = biome;
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
    */




    private float CalculateAverageHeight()
    {
        int totalVertices = 0;
        float totalHeight = 0;

        foreach (PointsForMap p in Points)
        {
            foreach ((int, int) pos in p.Indexes)
            {
                totalHeight += p.Map.Heights[pos.Item1, pos.Item2];
                totalVertices++;
            }
        }


        // Get the average height for all points
        return totalHeight / totalVertices;
    }






}
