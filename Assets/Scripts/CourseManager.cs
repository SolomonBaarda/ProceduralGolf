using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CourseManager : MonoBehaviour
{
    public GolfBall GolfBall;


    public TerrainChunkManager AllTerrain;
    public HashSet<Hole> GolfHoles;










    public void UpdateGolfHolesOrder()
    {
        if (GolfHoles.Count > 0)
        {
            /*
            // We have not assinged the first hole yet
            if (GolfHoles.((x) => x.Number != Hole.NotAssignedHoleNumber) == null)
            {
                // Set the centre hole to be the first hole
                Hole closest = GetClosestTo(TerrainGenerator.ORIGIN);
                closest.Number = 0;
            }


            // Sort the holes by number and distance
            GolfHoles.Sort((x, y) => CompareHoles(x, y));


            // Loop through them all
            for (int i = 0; i < GolfHoles.Count; i++)
            {
                // And assign the number
                if (GolfHoles[i].Number == Hole.NotAssignedHoleNumber)
                {
                    GolfHoles[i].Number = i;
                }
            }
            */
        }
    }


    private int CompareHoles(Hole a, Hole b)
    {
        // Both have numbers assigned to them 
        if (a.Number != Hole.NotAssignedHoleNumber && b.Number != Hole.NotAssignedHoleNumber)
        {
            return a.Number.CompareTo(b.Number);
        }
        // A has been assigned but not b
        else if (a.Number != Hole.NotAssignedHoleNumber && b.Number == Hole.NotAssignedHoleNumber)
        {
            return 1;
        }
        // B has been assigned but not A
        else if (a.Number == Hole.NotAssignedHoleNumber && b.Number != Hole.NotAssignedHoleNumber)
        {
            return -1;
        }
        // Neither have been assigned
        else if (a.Number == Hole.NotAssignedHoleNumber && b.Number == Hole.NotAssignedHoleNumber)
        {
            Hole origin = GetClosestTo(TerrainGenerator.ORIGIN);
            return Vector3.Distance(a.Centre, origin.Centre).CompareTo(Vector3.Distance(b.Centre, origin.Centre));
        }
        else
        {
            return 0;
        }
    }



    public void RespawnGolfBall()
    {
        RespawnGolfBall(CalculateSpawnPoint(GolfBall.Radius));
    }

    public void RespawnGolfBall(Vector3 position)
    {
        // Reset
        GolfBall.StopAllCoroutines();
        GolfBall.Reset();
        GolfBall.GameStats.Reset();

        // Position
        GolfBall.transform.position = position;
        // Freeze the ball
        GolfBall.WaitForNextShot();
    }




    private Hole GetClosestTo(Vector3 pos)
    {
        if (GolfHoles.Count > 0)
        {
            Hole closest = GolfHoles.FirstOrDefault();
            foreach (Hole h in GolfHoles)
            {
                if (Vector3.Distance(h.Centre, pos) < Vector3.Distance(closest.Centre, pos))
                {
                    closest = h;
                }
            }

            return closest;
        }

        return null;
    }



    public Vector3 CalculateSpawnPoint(float sphereRadius)
    {
        if (GolfHoles.Count > 0)
        {
            // Find the hole closest to the origin
            Hole closest = GetClosestTo(TerrainGenerator.ORIGIN);

            Destroy(closest.Flag);

            Vector3 ground = closest.Centre;
            ground += TerrainGenerator.UP * sphereRadius;

            return ground;
        }
        else
        {
            Debug.Log("There are no golf holes nearby so spawning at 0 0 0");

            TerrainChunk chunk = AllTerrain.GetChunk(Vector2Int.zero);
            Vector3 centre3 = chunk.Bounds.center;
            Vector2 centre = new Vector2(centre3.x, centre3.z);

            Vector3 closest = chunk.Collider.vertices[0];
            int index = 0;

            for (int i = 0; i < chunk.Collider.vertices.Length; i++)
            {
                // Update the closest point
                if (Vector2.Distance(centre, new Vector2(chunk.Collider.vertices[i].x, chunk.Collider.vertices[i].z)) < Vector3.Distance(centre, new Vector3(closest.x, closest.z)))
                {
                    closest = chunk.Collider.vertices[i];
                    index = i;
                }
            }


            Vector3 normal = chunk.Collider.normals[index];
            // Move the point in the direction of the normal a little
            closest += normal.normalized * sphereRadius;

            return closest;
        }
    }


}
