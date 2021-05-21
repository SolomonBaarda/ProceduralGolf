using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public HashSet<Vector3> Vertices = new HashSet<Vector3>(new VertexComparer());
    private Vector3 initialPos;
    public bool ToBeDeleted = false;

    public class Points
    {
        public TerrainMap Map;
        public HashSet<(int, int)> Indexes = new HashSet<(int, int)>();
    }

    private class VertexComparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b)
        {
            //const float threshold = 0.01f;
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
            //return FastApproximate(a.x, b.x, threshold) && FastApproximate(a.y, b.y, threshold) && FastApproximate(a.z, b.z, threshold);
        }

        public int GetHashCode(Vector3 v)
        {
            return v.GetHashCode();
        }

        private static bool FastApproximate(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }
    }

    public Green(Vector3 initialWorldPos)
    {
        initialPos = initialWorldPos;
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

    public Vector3 CalculateStart()
    {
        return CalculateCentre(out Vector3 _, out Vector3 _);
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
