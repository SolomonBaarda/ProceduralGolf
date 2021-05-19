using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public HashSet<Vector3> Vertices = new HashSet<Vector3>();
    private Vector3 initialPos;

    public Green(Vector3 initialWorldPos)
    {
        initialPos = initialWorldPos;
    }

    public void CheckPoint(Vector3 biome)
    {
        Vertices.Add(biome);
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


    public Vector3 CalculateCentre(out Vector3 min, out Vector3 max)
    {
        min = initialPos;
        max = min;

        foreach (Vector3 point in Vertices)
        {
            if (point.x < min.x) { min.x = point.x; }
            if (point.z < min.z) { min.z = point.z; }

            if (point.x > max.x) { max.x = point.x; }
            if (point.z > max.z) { max.z = point.z; }
        }

        Vector3 centreOffset = (max + min) / 2;

        return centreOffset;

    }




    /*
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
