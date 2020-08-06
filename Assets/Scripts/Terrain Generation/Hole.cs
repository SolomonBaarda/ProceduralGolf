using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole
{
    public List<TerrainMap.Point> HoleVertices;

    public Hole()
    {
        HoleVertices = new List<TerrainMap.Point>();
    }





    public Vector3 EvaluateMidpointLocal()
    {
        Vector3 min = HoleVertices[0].LocalVertexPosition, max = HoleVertices[0].LocalVertexPosition;

        foreach (TerrainMap.Point p in HoleVertices)
        {
            Vector3 v = p.LocalVertexPosition;

            if (v.x < min.x) { min.x = v.x; }
            if (v.z < min.z) { min.z = v.z; }

            if (v.x > max.x) { max.x = v.x; }
            if (v.z > max.z) { max.z = v.z; }
        }

        Vector3 centreOffset = (max - min) / 2;
        centreOffset.y = EvaluateHeight();

        return centreOffset;
    }


    public float EvaluateHeight()
    {
        int total = HoleVertices.Count;

        float totalHeight = 0;
        foreach (TerrainMap.Point p in HoleVertices)
        {
            totalHeight += p.Height;
        }

        return totalHeight / total;
    }










    private static bool HoleHasBeenCreated(in TerrainMap.Point p, out Hole h)
    {
        List<TerrainMap.Point> pointsAlreadyChecked = new List<TerrainMap.Point>();

        // Check the root first
        if (PointHasHole(ref pointsAlreadyChecked, p, out h))
        {
            return true;
        }
        // Now the recursive case
        else
        {
            return CheckAllNeighboursForHoleRecursive(ref pointsAlreadyChecked, p.Neighbours, out h);
        }
    }


    private static bool CheckAllNeighboursForHoleRecursive(ref List<TerrainMap.Point> pointsAlreadyChecked, List<TerrainMap.Point> neighbours, out Hole h)
    {
        // Check neighbours for holes
        foreach (TerrainMap.Point p in neighbours)
        {
            // Don't bother checking non holes
            if (p.Biome != TerrainSettings.Biome.Hole)
            {
                pointsAlreadyChecked.Add(p);
                continue;
            }

            // This neighbour has a hole
            if (PointHasHole(ref pointsAlreadyChecked, p, out h))
            {
                return true;
            }
        }

        // Check neighbours neighbours recursively
        foreach (TerrainMap.Point p in neighbours)
        {
            if (!pointsAlreadyChecked.Contains(p))
            {
                // Doesn't have a hole - recursive case
                if (CheckAllNeighboursForHoleRecursive(ref pointsAlreadyChecked, p.Neighbours, out h))
                {
                    return true;
                }
            }
        }


        h = null;
        return false;
    }

    private static bool PointHasHole(ref List<TerrainMap.Point> alreadyChecked, TerrainMap.Point p, out Hole h)
    {
        // Don't bother checking if it is not a hole
        if (p.Biome != TerrainSettings.Biome.Hole)
        {
            alreadyChecked.Add(p);
        }

        // This point needs to be checked
        if (!alreadyChecked.Contains(p))
        {
            alreadyChecked.Add(p);

            // If there is a hole
            if (p.Hole != null)
            {
                h = p.Hole;
                return true;
            }
        }

        h = null;
        return false;
    }




    public static List<Hole> CalculateHoles(ref TerrainMap t)
    {
        List<Hole> holes = new List<Hole>();

        for (int y = 0; y < t.Height; y++)
        {
            for (int x = 0; x < t.Width; x++)
            {
                // Vertex is part of a hole
                if (t.Map[x, y].Biome == TerrainSettings.Biome.Hole)
                {
                    // Need to create a hole
                    if (!HoleHasBeenCreated(t.Map[x, y], out Hole h))
                    {
                        h = new Hole();
                    }

                    // Add the vertex to this hole
                    h.HoleVertices.Add(t.Map[x, y]);

                    // Add the hole to the point
                    t.Map[x, y].Hole = h;

                    // Add this hole if we need to
                    if (!holes.Contains(h))
                    {
                        holes.Add(h);
                    }
                }
            }
        }

        return holes;
    }



}
