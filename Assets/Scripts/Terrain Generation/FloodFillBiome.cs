using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloodFillBiome
{
    public Biome.Type Biome;

    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;

    /*
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


            SetAllVerticesConnectedToThis();

            NeedsUpdating = true;
        }
    }


    public void SetAllVerticesConnectedToThis()
    {
        // Assign the points hole to be this
        foreach (TerrainMap.Point p in Vertices)
        {
            p.Connected = this;
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











    private static void GetAllConnectedPointsWorker(TerrainMap.Point p, ref HashSet<TerrainMap.Point> pointsInThisFlood, ref HashSet<FloodFillBiome> biomesFound, Biome.Type biome)
    {
        // Ensure we start with a new hole point
        if (!pointsInThisFlood.Contains(p) && p.Biome == biome)
        {
            pointsInThisFlood.Add(p);

            // If this is a new biome then add it
            if (p.Connected != null && !biomesFound.Contains(p.Connected))
            {
                biomesFound.Add(p.Connected);

                pointsInThisFlood.UnionWith(p.Connected.Vertices);
            }

            // Then check each neighbour
            foreach (TerrainMap.Point neighbour in p.Neighbours)
            {
                if (!pointsInThisFlood.Contains(neighbour) && neighbour.Biome == biome)
                {
                    GetAllConnectedPointsWorker(neighbour, ref pointsInThisFlood, ref biomesFound, biome);
                }
                    
            }
        }

    }


    private static HashSet<TerrainMap.Point> GetAllConnectedPoints(TerrainMap.Point start, out HashSet<FloodFillBiome> biomesFound, Biome.Type biome)
    {
        biomesFound = new HashSet<FloodFillBiome>();
        HashSet<TerrainMap.Point> pointsInThisFlood = new HashSet<TerrainMap.Point>();


        // Calculate all the hole points
        GetAllConnectedPointsWorker(start, ref pointsInThisFlood, ref biomesFound, biome); ;

        return pointsInThisFlood;
    }



    private static void CheckPoint(TerrainMap.Point p, ref HashSet<TerrainMap.Point> pointsAlreadyChecked, ref HashSet<FloodFillBiome> floodsFound, Biome.Type biome)
    {
        if (!pointsAlreadyChecked.Contains(p))
        {
            pointsAlreadyChecked.Add(p);

            // Vertex is the correct biome
            if (p.Biome == biome)
            {

                // Do the flood fill for all the connected points
                if (p.Connected == null)
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
                        floodsFound.Add(h);

                        h.SetAllVerticesConnectedToThis();
                    }
                    // Multiple floods found - need to merge them
                    else
                    {
                        List<FloodFillBiome> floodsToMerge = floods.ToList();

                        // Merge the floods until there is only one left
                        while (floodsToMerge.Count >= 2)
                        {
                            FloodFillBiome toMerge = floodsToMerge[1];
                            floodsToMerge[0].Merge(ref toMerge);
                            floodsToMerge.Remove(toMerge);
                        }

                        // When we get here, there is only one flood left so just add it
                        floodsFound.UnionWith(floodsToMerge);
                    }
                }
                // Don't bother doing the flood - it has already been done
                else
                {
                    floodsFound.Add(p.Connected);
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
        holes.RemoveWhere((x) => x.ShouldBeDestroyed || x.Vertices.Count == 0);

        return holes;
    }

    */
}
