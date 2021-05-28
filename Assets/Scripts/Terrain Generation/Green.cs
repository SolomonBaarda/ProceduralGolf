using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public List<Point> Points = new List<Point>();
    public List<Point> PointsOnEdge = new List<Point>();
    public bool ToBeDeleted = false;

    public class Point
    {
        public TerrainMap Map;
        public int indexX, indexY;
    }
}
