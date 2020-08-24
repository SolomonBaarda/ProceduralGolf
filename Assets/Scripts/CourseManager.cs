using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class CourseManager : MonoBehaviour
{
    public GolfBall GolfBall;
    public TerrainManager TerrainManager;

    public bool HolesHaveBeenOrdered { get; private set; }


    private Dictionary<int, HoleData> Holes = new Dictionary<int, HoleData>();
    private HoleData CurrentNext;

    public UnityAction<GolfBall.Stats> OnHoleCompleted;


    private void Awake()
    {
        OnHoleCompleted += Utils.EMPTY;
    }

    private void OnDestroy()
    {
        OnHoleCompleted -= Utils.EMPTY;
    }




    private void FixedUpdate()
    {
        if (CurrentNext != null)
        {
            /*
            if (CurrentNext.BallWasPotted(GolfBall.Mask))
            {
                CurrentNext.SetCompleted();

                HoleData next = GetHole(CurrentNext.Number + 1);

                GolfBall.HoleCompleted(next);
                RespawnGolfBall(next);
            }
            */
        }

    }


    public void UpdateGolfHoles(List<HoleData> holes)
    {
        // Get all of the holes on the course
        HashSet<HoleData> allHolesHash = new HashSet<HoleData>(Holes.Values);
        allHolesHash.UnionWith(holes);

        if (allHolesHash.Count > 0)
        {
            List<HoleData> allHoles = allHolesHash.ToList();

            // If we need to assign the first hole
            if (!GetHole(0, out HoleData _))
            {
                // Set the centre hole to be the first hole
                if(GetClosestTo(TerrainManager.ORIGIN, allHoles, out HoleData closest))
                {
                    allHoles.Remove(closest);

                    closest.Number = 0;
                    Holes.Add(closest.Number, closest);
                }
                else
                {
                    Debug.LogError("Could not set hole 0.");
                }
            }

            allHoles.RemoveAll((x) => Holes.Values.Contains(x));


            while (allHoles.Count > 0)
            {
                // Sort the holes by number and distance
                allHoles.Sort((x, y) => CompareHoleDistanceTo(x, y, TerrainManager.ORIGIN));

                HoleData h = allHoles[0];
                allHoles.Remove(h);

                h.Number = GetNextHoleNumber(Holes.Keys);
                Holes.Add(h.Number, h);
            }

            HolesHaveBeenOrdered = true;
        }
    }




    public bool GetHole(int number, out HoleData hole)
    {
        return Holes.TryGetValue(number, out hole);
    }


    private int GetNextHoleNumber(IEnumerable<int> currentHoles)
    {
        return currentHoles.Max() + 1;
    }



    private int CompareHoleDistanceTo(HoleData a, HoleData b, Vector3 position)
    {
        return (position - a.Centre).sqrMagnitude.CompareTo((position - b.Centre).sqrMagnitude);
    }




    public void RespawnGolfBall(HoleData hole)
    {
        // Get the next hole
        if (GetHole(hole.Number + 1, out HoleData next))
        {
            GolfBall.ResetAllStats(next);
            CurrentNext = next;

            // And move the ball there
            RespawnGolfBall(TerrainManager.CalculateSpawnPoint(GolfBall.Radius, hole.Centre));

            Debug.Log("Respawned ball at hole " + hole.Number + " and set next hole to be " + next.Number);
        }
        else
        {
            Debug.LogError("Could not respawn Golfball as there are no more Holes.");
        }
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




    private bool GetClosestTo(Vector3 pos, IEnumerable<HoleData> collection, out HoleData closest)
    {
        closest = null;

        // Get the closest from the list
        if (collection.Count() > 0)
        {
            closest = collection.FirstOrDefault();

            foreach (HoleData h in collection)
            {
                float posMag = pos.sqrMagnitude;
                if (h.Centre.sqrMagnitude - posMag < closest.Centre.sqrMagnitude - posMag)
                {
                    closest = h;
                }
            }

            return true;
        }

        return false;
    }











    public void Clear()
    {
        Holes.Clear();
        CurrentNext = null;
        HolesHaveBeenOrdered = false;
    }


}
