using System.Collections.Generic;
using UnityEngine;

public class Green
{
    public List<Point> Points = new List<Point>();
    public List<Point> PointsOnEdge = new List<Point>();
    public bool ToBeDeleted = false;

    public Vector3 Start, Hole;
    public List<Vector3> PossibleHoles;

    public class Point
    {
        public Vector2Int Chunk;
        public int indexX, indexY;
    }


}
