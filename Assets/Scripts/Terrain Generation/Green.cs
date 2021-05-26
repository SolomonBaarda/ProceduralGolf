using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public List<Point> Points = new List<Point>();
    public List<Point> PointsOnEdge = new List<Point>();
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
        //List<Vector3> list = new List<Vector3>(Vertices);
        //return list[new System.Random().Next(0, list.Count)];
        return Vector3.zero;
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


    public class Point
    {
        public TerrainMap Map;
        public int indexX, indexY;

        public static bool IsValidNeighbour(Point a, Point b)
        {
            bool validX, validY;

            // Left
            if (a.Map.Chunk.x - 1 == b.Map.Chunk.x)
            {
                if (a.indexX == 0 || b.indexX == a.Map.Width - 1)
                {
                    validX = true;
                }
                else
                {
                    return false;
                }
            }
            // Right
            else if (a.Map.Chunk.x + 1 == b.Map.Chunk.x)
            {
                if (a.indexX == a.Map.Width - 1 || b.indexX == 0)
                {
                    validX = true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Up
            if (a.Map.Chunk.y - 1 == b.Map.Chunk.y)
            {
                if (a.indexY == 0 || b.indexY == a.Map.Height - 1)
                {
                    validY = true;
                }
                else
                {
                    return false;
                }
            }
            // Down
            else if (a.Map.Chunk.y + 1 == b.Map.Chunk.y)
            {
                if (a.indexY == a.Map.Height - 1 || b.indexY == 0)
                {
                    validY = true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return validX || validY;
        }
    }

}
