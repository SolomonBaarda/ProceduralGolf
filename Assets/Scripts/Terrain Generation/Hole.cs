using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hole
{
    public const int NotAssignedHoleNumber = -1;
    public const float LinePreviewHeight = 100f;
    public int Number { get; private set; } = NotAssignedHoleNumber;

    public bool Completed { get; private set; } = false;


    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;


    public HashSet<TerrainMap.Point> Vertices = new HashSet<TerrainMap.Point>();
    public Vector3 Centre => EvaluateMidpoint();

    public GameObject WorldObjectParent;
    private GameObject Flag;
    private GameObject LinePreviewObject;


    public void Destroy()
    {
        Vertices.Clear();

        if (WorldObjectParent != null)
        {
            UnityEngine.Object.Destroy(WorldObjectParent);
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


    public bool BallWasPotted(int layerMask)
    {
        float radius = 0.2f;

        return Physics.CheckSphere(Centre, radius, layerMask);
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


    public void UpdateHole(Transform parent, GameObject flagPrefab, GameObject linePrefab)
    {
        NeedsUpdating = false;

        // Update the heights
        SetAllPointHeights();

        if (WorldObjectParent == null)
        {
            WorldObjectParent = new GameObject("Hole " + Number);
            WorldObjectParent.transform.parent = parent;
            WorldObjectParent.transform.position = Centre;
        }

        if (!Completed)
        {
            if (Flag == null)
            {
                Flag = UnityEngine.Object.Instantiate(flagPrefab, WorldObjectParent.transform);
                Flag.transform.localPosition = (TerrainGenerator.UP * Flag.GetComponent<Collider>().bounds.extents.y);
            }

            if (LinePreviewObject == null)
            {
                LinePreviewObject = UnityEngine.Object.Instantiate(linePrefab, WorldObjectParent.transform);
                LinePreview l = LinePreviewObject.GetComponent<LinePreview>();
                l.SetPoints(Centre, Centre + (TerrainGenerator.UP * LinePreviewHeight));

                LinePreviewObject.SetActive(false);
            }
        }

    }


    public void SetNumber(int number)
    {
        Number = number;

        if (WorldObjectParent != null)
        {
            WorldObjectParent.name = "Hole " + Number;
        }
    }



    public void SetNext()
    {
        LinePreviewObject.SetActive(true);
    }



    public void SetCompleted()
    {
        Completed = true;

        if (Flag != null)
        {
            UnityEngine.Object.Destroy(Flag);
        }

        if (LinePreviewObject != null)
        {
            LinePreviewObject.SetActive(false);
        }
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


    private static void GetAllConnectedHolePointsWorker(TerrainMap.Point start, ref HashSet<TerrainMap.Point> connected, ref HashSet<Hole> holesFound)
    {
        // Ensure we start with a new hole point
        if (start.Biome == Biome.Type.Hole && !connected.Contains(start))
        {
            connected.Add(start);

            // If this is a new hole found, then add it
            if (start.Hole != null && !holesFound.Contains(start.Hole))
            {
                holesFound.Add(start.Hole);
            }

            // Then check each neighbour
            foreach (TerrainMap.Point p in start.Neighbours)
            {
                GetAllConnectedHolePointsWorker(p, ref connected, ref holesFound);
            }
        }
    }


    private static HashSet<TerrainMap.Point> GetAllConnectedHolePoints(TerrainMap.Point start, out HashSet<Hole> anyHolesFound)
    {
        anyHolesFound = new HashSet<Hole>();
        HashSet<TerrainMap.Point> points = new HashSet<TerrainMap.Point>();

        // Calculate all the hole points
        GetAllConnectedHolePointsWorker(start, ref points, ref anyHolesFound);

        return points;
    }



    private static void CheckPoint(TerrainMap.Point p, ref HashSet<TerrainMap.Point> pointsAlreadyChecked, ref HashSet<Hole> holes)
    {
        if (!pointsAlreadyChecked.Contains(p))
        {
            pointsAlreadyChecked.Add(p);

            // Vertex is part of a hole
            if (p.Biome == Biome.Type.Hole)
            {
                HashSet<TerrainMap.Point> pointsInThisHole = GetAllConnectedHolePoints(p, out HashSet<Hole> holesFound);

                // We have checked all the points in this hole now, don't do it again
                pointsAlreadyChecked.UnionWith(pointsInThisHole);


                // No holes found - need to create a new one
                if (holesFound.Count == 0)
                {
                    // Make a new hole
                    Hole h = new Hole();

                    // Add all the vertices
                    h.Vertices.UnionWith(pointsInThisHole);
                    // Add this hole
                    holes.Add(h);

                    // Set the hole for each point
                    foreach (TerrainMap.Point point in pointsInThisHole)
                    {
                        point.Hole = h;
                    }
                }
                // Multiple holes found - need to merge them
                else
                {
                    List<Hole> holesToMerge = holesFound.ToList();

                    // Merge the holes until there is only one left
                    while (holesToMerge.Count > 1)
                    {
                        Hole toMerge = holesToMerge[1];
                        holesToMerge[0].Merge(ref toMerge);
                        holesToMerge.Remove(toMerge);
                    }

                    // When we get here, there is only one hole left so just add it
                    holes.UnionWith(holesToMerge);
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
            for (int x = 0; x < t.Width; x++)
            {
                CheckPoint(t.Map[x, y], ref alreadyChecked, ref holes);
            }
        }

        // Remove holes that we don't need
        holes.RemoveWhere((x) => x.Vertices.Count == 0 || x.ShouldBeDestroyed);

        //Debug.Log("Calculating and merging holes: " + (DateTime.Now - before).Milliseconds);

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
