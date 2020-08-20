using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class CourseManager : MonoBehaviour
{
    public GolfBall GolfBall;


    public TerrainChunkManager AllTerrain;
    public HashSet<Hole> GolfHoles;

    public bool HolesHaveBeenOrdered;

    public Hole GetHole(int number) { Holes.TryGetValue(number, out Hole value); return value; }
    private Dictionary<int, Hole> Holes = new Dictionary<int, Hole>();

    public UnityAction<GolfBall.Stats> OnHoleCompleted;

    public int CurrentHole = 0;

    private void Awake()
    {
        OnHoleCompleted += Utils.EMPTY;
    }

    private void OnDestroy()
    {
        OnHoleCompleted -= Utils.EMPTY;
    }





    public void UpdateGolfHolesOrder()
    {
        if (GolfHoles.Count > 0)
        {
            // Get all the new holes
            List<Hole> AllHoles = GolfHoles.ToList();

            // If we need to assign the first hole
            if (GetHole(0) == null)
            {
                // Set the centre hole to be the first hole
                Hole closest = GetClosestTo(TerrainGenerator.ORIGIN, AllHoles);
                AllHoles.Remove(closest);

                closest.Number = 0;
                Holes.Add(closest.Number, closest);
            }

            AllHoles.RemoveAll((x) => Holes.Values.Contains(x));


            while (AllHoles.Count > 0)
            {
                // Sort the holes by number and distance
                AllHoles.Sort((x, y) => CompareHoles(x, y));

                Hole h = AllHoles[0];
                AllHoles.Remove(h);

                h.Number = GetNextHoleNumber(Holes.Keys);
                Holes.Add(h.Number, h);
            }

            HolesHaveBeenOrdered = true;
        }
    }



    private int GetNextHoleNumber(IEnumerable<int> currentHoles)
    {
        return currentHoles.Max() + 1;
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
            return (TerrainGenerator.ORIGIN - a.Centre).sqrMagnitude.CompareTo((TerrainGenerator.ORIGIN - b.Centre).sqrMagnitude);
        }
        else
        {
            return 0;
        }
    }



    public void RespawnGolfBall(int hole)
    {
        // Get the next hole
        Hole next = GetHole(hole + 1);
        GolfBall.ResetAllStats(next);

        // And move the ball there
        Vector3 pos = CalculateSpawnPoint(GolfBall.Radius, hole);
        RespawnGolfBall(pos);

        Debug.Log("Respawned ball at hole " + hole + " and set next hole to be " + next.Number);
    }


    public void RespawnGolfBall(Vector3 position)
    {
        // Reset
        GolfBall.StopAllCoroutines();
        GolfBall.Reset();

        // Position
        GolfBall.transform.position = position;
        // Freeze the ball
        GolfBall.WaitForNextShot();
    }




    private Hole GetClosestTo(Vector3 pos, IEnumerable<Hole> collection)
    {
        if (GolfHoles.Count > 0)
        {
            Hole closest = collection.FirstOrDefault();
            foreach (Hole h in collection)
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



    public Vector3 CalculateSpawnPoint(float sphereRadius, int hole)
    {
        Hole h = GetHole(hole);
        if (h != null)
        {
            if (h.Flag != null)
            {
                Destroy(h.Flag);
            }

            Vector3 groundPos = h.Centre;
            groundPos += TerrainGenerator.UP * sphereRadius;

            return groundPos;
        }

        return default;
    }






    public void Clear()
    {
        Holes.Clear();
        HolesHaveBeenOrdered = false;
    }


}
