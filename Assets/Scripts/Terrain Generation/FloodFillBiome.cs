using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloodFillBiome
{
    public Biome.Type Biome;

    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;


    public HashSet<TerrainMap.Point> Vertices = new HashSet<TerrainMap.Point>();
    public Vector3 Centre => EvaluateMidpoint();

    public FloodFillBiome(Biome.Type biome)
    {
        Biome = biome;
    }



    public void Destroy()
    {
        Vertices.Clear();
    }



    private Vector3 EvaluateMidpoint()
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




    private float EvaluateHeight()
    {
        int total = Vertices.Count;

        float totalHeight = 0;
        foreach (TerrainMap.Point p in Vertices)
        {
            totalHeight += p.OriginalHeight;
        }

        // Get the average height for all points
        return totalHeight / total;
    }



    public void Update()
    {
        NeedsUpdating = false;

        // Update the heights
        SetAllPointHeights();
    }




    public void Merge(ref FloodFillBiome points)
    {
        if (this != points && Biome == points.Biome)
        {
            // Add all the vertices to this object and remove it from the other
            Vertices.UnionWith(points.Vertices);
            points.Vertices.Clear();

            points.ShouldBeDestroyed = true;

            // Assign the points hole to be this
            foreach (TerrainMap.Point p in Vertices)
            {
                p.Connected = this;
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








    private static void GetAllConnectedPointsWorker(TerrainMap.Point start, ref HashSet<TerrainMap.Point> connected, ref HashSet<FloodFillBiome> holesFound, Biome.Type biome)
    {
        // Ensure we start with a new hole point
        if (start.Biome == biome && !connected.Contains(start))
        {
            connected.Add(start);

            // If this is a new hole found, then add it
            if (start.Connected != null && !holesFound.Contains(start.Connected))
            {
                holesFound.Add(start.Connected);
            }

            // Then check each neighbour
            foreach (TerrainMap.Point p in start.Neighbours)
            {
                GetAllConnectedPointsWorker(p, ref connected, ref holesFound, biome);
            }
        }
    }


    private static HashSet<TerrainMap.Point> GetAllConnectedPoints(TerrainMap.Point start, out HashSet<FloodFillBiome> anyHolesFound, Biome.Type biome)
    {
        anyHolesFound = new HashSet<FloodFillBiome>();
        HashSet<TerrainMap.Point> points = new HashSet<TerrainMap.Point>();

        // Calculate all the hole points
        GetAllConnectedPointsWorker(start, ref points, ref anyHolesFound, biome);

        return points;
    }



    private static void CheckPoint(TerrainMap.Point p, ref HashSet<TerrainMap.Point> pointsAlreadyChecked, ref HashSet<FloodFillBiome> holes, Biome.Type biome)
    {
        if (!pointsAlreadyChecked.Contains(p))
        {
            pointsAlreadyChecked.Add(p);

            // Vertex is the correct biome
            if (p.Biome == biome)
            {
                HashSet<TerrainMap.Point> pointsInThisFlood = GetAllConnectedPoints(p, out HashSet<FloodFillBiome> floods, biome);

                // We have checked all the points in this hole now, don't do it again
                pointsAlreadyChecked.UnionWith(pointsInThisFlood);


                // No holes found - need to create a new one
                if (floods.Count == 0)
                {
                    // Make a new hole
                    FloodFillBiome h = new FloodFillBiome(biome);

                    // Add all the vertices
                    h.Vertices.UnionWith(pointsInThisFlood);
                    // Add this hole
                    holes.Add(h);

                    // Set the hole for each point
                    foreach (TerrainMap.Point point in pointsInThisFlood)
                    {
                        point.Connected = h;
                    }
                }
                // Multiple holes found - need to merge them
                else
                {
                    List<FloodFillBiome> holesToMerge = floods.ToList();

                    // Merge the holes until there is only one left
                    while (holesToMerge.Count > 1)
                    {
                        FloodFillBiome toMerge = holesToMerge[1];
                        holesToMerge[0].Merge(ref toMerge);
                        holesToMerge.Remove(toMerge);
                    }

                    // When we get here, there is only one hole left so just add it
                    holes.UnionWith(holesToMerge);
                }
            }
        }
    }


    public static HashSet<FloodFillBiome> CalculatePoints(ref TerrainMap t, Biome.Type biome)
    {
        HashSet<FloodFillBiome> holes = new HashSet<FloodFillBiome>();
        HashSet<TerrainMap.Point> alreadyChecked = new HashSet<TerrainMap.Point>();

        // Check each point
        for (int y = 0; y < t.Height; y++)
        {
            for (int x = 0; x < t.Width; x++)
            {
                CheckPoint(t.Points[x, y], ref alreadyChecked, ref holes, biome);
            }
        }

        // Remove ones that we don't need
        holes.RemoveWhere((x) => x.Vertices.Count == 0 || x.ShouldBeDestroyed);

        return holes;
    }


}
