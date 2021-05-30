using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public List<Point> Points = new List<Point>();
    public List<Point> PointsOnEdge = new List<Point>();
    public bool ToBeDeleted = false;

    public List<Hole> Holes = new List<Hole>();

    public class Point
    {
        public TerrainMap Map;
        public int indexX, indexY;
    }

    public class Hole
    {
        public Vector3 WorldCentre;
        public List<Point> Points;
    }
}
