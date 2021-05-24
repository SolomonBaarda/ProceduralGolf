using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public HashSet<Vector3> Vertices = new HashSet<Vector3>(new VertexComparer());
    private Vector3 initialPos;
    public bool ToBeDeleted = false;

    /// <summary>
    /// Custom comparer used for comparing Vector3 vertices. This takes into consideration float innacuracies 
    /// </summary>
    private class VertexComparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        public int GetHashCode(Vector3 v)
        {
            int hashCode = 1474027755;
            hashCode = hashCode * -1521134295 + Mathf.Round(v.x).GetHashCode();
            hashCode = hashCode * -1521134295 + Mathf.Round(v.y).GetHashCode();
            hashCode = hashCode * -1521134295 + Mathf.Round(v.z).GetHashCode();
            return hashCode;
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
