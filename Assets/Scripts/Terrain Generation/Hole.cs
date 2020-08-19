using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hole
{
    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;


    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;


    public HashSet<TerrainMap.Point> Vertices = new HashSet<TerrainMap.Point>();
    public Vector3 Centre => EvaluateMidpoint();

    public GameObject Flag;



    public void Destroy()
    {
        Vertices.Clear();

        if (Flag != null)
        {
            UnityEngine.Object.Destroy(Flag);
        }
    }



    public Vector3 EvaluateMidpoint()
    {
        if (Vertices.Count > 0)
        {
            TerrainMap.Point random = Vertices.FirstOrDefault();
            Vector3 min = random.LocalVertexPosition + random.Offset, max = min;

            foreach (TerrainMap.Point p in Vertices)
            {
                Vector3 v = p.LocalVertexPosition + p.Offset;

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
        foreach (TerrainMap.Point p in Vertices)
        {
            totalHeight += p.OriginalHeight;
        }

        // Get the average height for all points in the Hole
        return totalHeight / total;
    }


    public void UpdateHole()
    {
        NeedsUpdating = false;

        // Update the heights
        SetAllPointHeights();

        Flag.transform.position = Centre + (TerrainGenerator.UP * Flag.GetComponent<Collider>().bounds.extents.y);
    }



    public void Merge(ref Hole hole)
    {
        if (this != hole)
        {
            // Add all the vertices to this hole and remove it from the other
            Vertices.UnionWith(hole.Vertices);
            hole.Vertices.Clear();

            hole.ShouldBeDestroyed = true;

            // Assign the points hole to be this
            foreach (TerrainMap.Point p in Vertices)
            {
                p.Hole = this;
            }


            NeedsUpdating = true;
        }
    }


    public void SetAllPointHeights()
    {
        float height = EvaluateHeight();

        foreach (TerrainMap.Point p in Vertices)
        {
            p.Height = height;
        }
    }





    private static void CheckNeighboursForHole(TerrainMap.Point p, HashSet<TerrainMap.Point> alreadyChecked, HashSet<Hole> holes)
    {
        // Check this point - base case
        if (!alreadyChecked.Contains(p))
        {
            alreadyChecked.Add(p);

            // Vertex is part of a hole
            if (p.Biome == Biome.Type.Hole)
            {

                // Hole has not been assigned
                if (p.Hole == null)
                {
                    // Check all neighbours for a hole
                    foreach (TerrainMap.Point neighbour in p.Neighbours)
                    {
                        // Neighbour has a hole assigned
                        if (neighbour.Biome == Biome.Type.Hole && neighbour.Hole != null)
                        {
                            Hole h = neighbour.Hole;
                            holes.Add(h);

                            // Add this point to the hole
                            p.Hole = h;
                            h.Vertices.Add(p);

                            //alreadyChecked.Add(neighbour);

                            // Only bother checking one hole
                            break;
                        }
                    }

                    // If we get here, then we need to create a new hole
                    if (p.Hole == null)
                    {
                        Hole h = new Hole();

                        // Add this point to the hole
                        p.Hole = h;
                        h.Vertices.Add(p);

                        holes.Add(h);
                    }
                }
                // Has been assigned
                else
                {
                    Hole h = p.Hole;
                    holes.Add(h);

                    // Merge hole with neighbours
                    bool removedSomeHoles = false;
                    foreach (TerrainMap.Point neighbour in p.Neighbours)
                    {
                        // Neighbour is part of a different hole
                        if (neighbour.Biome == Biome.Type.Hole && neighbour.Hole != null && neighbour.Hole != h)
                        {
                            // Merge the holes together - means we don't need to check them again
                            h.Merge(ref neighbour.Hole);
                            alreadyChecked.UnionWith(h.Vertices);

                            Debug.Log("merged some holes");

                            removedSomeHoles = true;
                        }
                    }

                    // Do some cleanup if we need to
                    if (removedSomeHoles)
                    {
                        // Remove all holes with no vertices
                        holes.RemoveWhere((x) => x.Vertices.Count == 0 || x.ShouldBeDestroyed);
                    }
                }
            }
        }
    }


    public static NewHoles CalculateHoles(ref TerrainMap t)
    {
        DateTime before = DateTime.Now;

        HashSet<Hole> holes = new HashSet<Hole>();
        HashSet<TerrainMap.Point> alreadyChecked = new HashSet<TerrainMap.Point>();

        // Check each point
        for (int y = 0; y < t.Height; y++)
        {
            for(int x = 0; x < t.Width; x++) 
            {
                CheckNeighboursForHole(t.Map[x, y], alreadyChecked, holes);
            }
        }



        // Remove holes that we don't need
        holes.RemoveWhere((x) => x.Vertices.Count == 0 || x.ShouldBeDestroyed);

        //Debug.Log("Took " + (DateTime.Now - before).Milliseconds + " ms to find all " + holes.Count + " holes in chunk.");

        return new NewHoles(t, holes.ToList());
    }



    public struct NewHoles
    {
        public TerrainMap TerrainMap;
        public List<Hole> Holes;

        public NewHoles(in TerrainMap t, in List<Hole> h)
        {
            TerrainMap = t;
            Holes = h;
        }
    }

}
