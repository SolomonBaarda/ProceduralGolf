using System.Collections.Generic;
using UnityEngine;

public class Hole
{
    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;

    public List<TerrainMapGenerator.Point> Vertices = new List<TerrainMapGenerator.Point>();
    public Vector3 Centre => EvaluateMidpoint();

    public GameObject Flag;




    public void Destroy()
    {
        Vertices.Clear();

        if(Flag != null)
        {
            Object.Destroy(Flag);
        }
    }



    public Vector3 EvaluateMidpoint()
    {
        if(Vertices.Count > 0)
        {
            Vector3 min = Utils.FromV3(Vertices[0].LocalVertexPosition) + Utils.FromV3(Vertices[0].Offset), max = min;

            foreach (TerrainMapGenerator.Point p in Vertices)
            {
                Vector3 v = Utils.FromV3(p.LocalVertexPosition) + Utils.FromV3(p.Offset);

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


    public float EvaluateHeight()
    {
        int total = Vertices.Count;

        float totalHeight = 0;
        foreach (TerrainMapGenerator.Point p in Vertices)
        {
            totalHeight += p.OriginalHeight;
        }

        // Get the average height for all points in the Hole
        return totalHeight / total;
    }


    public void UpdateHole()
    {
        // Update the heights
        SetAllPointHeights();

        Flag.transform.position = Centre + (TerrainGenerator.UP * Flag.GetComponent<Collider>().bounds.extents.y);
    }



    public void Merge(ref Hole hole)
    {
        if (this != hole)
        {
            // Add all the vertices to this hole and remove it from the other
            Vertices.AddRange(hole.Vertices);
            hole.Vertices.Clear();
            hole.Destroy();

            // Assign the points hole to be this
            for (int i = 0; i < Vertices.Count; i++)
            {
                TerrainMapGenerator.Point p = Vertices[i];
                p.Hole = hole;
                Vertices[i] = p;
            }

            UpdateHole();
        }
    }


    public void SetAllPointHeights()
    {
        float height = EvaluateHeight();

        for (int i = 0; i < Vertices.Count; i++)
        {
            TerrainMapGenerator.Point p = Vertices[i];
            p.Height = height;
            Vertices[i] = p;
        }
    }


    private static bool HoleHasBeenCreated(in TerrainMapGenerator.Point p, out Hole h)
    {
        List<TerrainMapGenerator.Point> pointsAlreadyChecked = new List<TerrainMapGenerator.Point>();

        if (p.Biome == TerrainSettings.Biome.Hole)
        {
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

        h = null;
        return false;
    }


    private static bool CheckAllNeighboursForHoleRecursive(ref List<TerrainMapGenerator.Point> pointsAlreadyChecked, List<TerrainMapGenerator.Point> neighbours, out Hole h)
    {
        // Check neighbours for holes
        foreach (TerrainMapGenerator.Point neighbour in neighbours)
        {
            // This neighbour has a hole assigned
            if (PointHasHole(ref pointsAlreadyChecked, neighbour, out h))
            {
                return true;
            }
        }

        // Check neighbours neighbours recursively
        foreach (TerrainMapGenerator.Point neighbour in neighbours)
        {
            List<TerrainMapGenerator.Point> neighboursNotChecked = new List<TerrainMapGenerator.Point>();

            // Get all the neighbours that have not already been checked
            foreach (TerrainMapGenerator.Point neighbourOfNeighbour in neighbour.Neighbours)
            {
                if (neighbourOfNeighbour.Biome == TerrainSettings.Biome.Hole && !pointsAlreadyChecked.Contains(neighbourOfNeighbour))
                {
                    neighboursNotChecked.Add(neighbourOfNeighbour);
                }
            }

            // Doesn't have a hole - recursive case
            if (CheckAllNeighboursForHoleRecursive(ref pointsAlreadyChecked, neighboursNotChecked, out h))
            {
                return true;
            }
        }


        h = null;
        return false;
    }

    private static bool PointHasHole(ref List<TerrainMapGenerator.Point> alreadyChecked, TerrainMapGenerator.Point p, out Hole h)
    {
        // Don't bother checking if it is not a hole
        if (p.Biome != TerrainSettings.Biome.Hole)
        {
            alreadyChecked.Add(p);
        }
        // It is a hole
        else
        {
            // This point needs to be checked
            if (!alreadyChecked.Contains(p))
            {
                alreadyChecked.Add(p);

                // If there is a hole assigned to that point
                if (p.Hole != null)
                {
                    h = p.Hole;
                    return true;
                }
            }
        }

        h = null;
        return false;
    }




    public static List<Hole> CalculateHoles(ref TerrainMapGenerator.TerrainMap t)
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
                    h.Vertices.Add(t.Map[x, y]);

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
