using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using System;

public class CourseManager : MonoBehaviour
{
    public GolfBall GolfBall;
    public TerrainManager TerrainManager;

    public bool HolesHaveBeenOrdered { get; private set; }


    private Dictionary<int, HoleData> Holes = new Dictionary<int, HoleData>();
    public int NumberOfHoles => Holes.Count;

    public UnityAction OnHoleCompleted;


    [Header("Prefabs")]
    public GameObject GolfHoleFlagPrefab;
    public GameObject GolfHoleBeaconPrefab;
    private LinePreview NextHoleBeacon;


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
        // Get the next hole
        if (CalculateNextHole(out HoleData target))
        {
            // Ball was potted this frame
            if (target.BallWasPotted(GolfBall.Mask))
            {
                // Respawn the ball here
                RespawnGolfBall(target);

                // Call the event once the ball has been respawned
                OnHoleCompleted.Invoke();
            }

            // Ensure the beacon is always active
            if (NextHoleBeacon == null)
            {
                GameObject beaconObject = Instantiate(GolfHoleBeaconPrefab, transform);
                beaconObject.name = "Next hole beacon";
                NextHoleBeacon = beaconObject.GetComponent<LinePreview>();
            }

            // Set the position
            NextHoleBeacon.transform.position = target.Centre;

            // Calculate the beacon width
            float distanceSqr = (target.Centre - GolfBall.Position).sqrMagnitude;
            float maximumDistance = TerrainChunkManager.ChunkSizeWorldUnits * 2;
            float percent = Mathf.Clamp(distanceSqr / (maximumDistance * maximumDistance), 0f, 1f);
            
            // Set the width
            NextHoleBeacon.UpdateLineWidth(Mathf.Lerp(0.05f, 10, percent));


            // Set the points
            NextHoleBeacon.SetPoints(target.Centre, TerrainManager.UP);
        }

    }


    private bool CalculateNextHole(out HoleData nextHole)
    {
        return Holes.TryGetValue(GolfBall.Progress.LastHoleReached + 1, out nextHole);
    }


    public void UpdateGolfHoles(IEnumerable<HoleData> holes)
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
                if (GetClosestTo(TerrainManager.ORIGIN, allHoles, out HoleData closest))
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


    public void Restart()
    {
        if (GetHole(0, out HoleData start))
        {
            GolfBall.Progress.Clear();

            RespawnGolfBall(start);
        }
        else
        {
            Debug.LogError("Could not respawn GolfBall as there is no first Hole.");
        }
    }


    public void RespawnGolfBall(HoleData hole)
    {
        GolfBall.HoleReached(hole, DateTime.Now);

        // And move the ball there
        MoveGolfBallAndWaitForNextShot(TerrainManager.CalculateSpawnPoint(GolfBall.Radius, hole.Centre));
    }


    private void MoveGolfBallAndWaitForNextShot(Vector3 position)
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
        HolesHaveBeenOrdered = false;
    }


}
