using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public HashSet<Vector3> Vertices = new HashSet<Vector3>(new VertexComparer());
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


    public Vector3 CalculateStart()
    {
        List<Vector3> list = new List<Vector3>(Vertices);
        return list[new System.Random().Next(0, list.Count)];
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
