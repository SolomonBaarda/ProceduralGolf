using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TerrainManager : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;

    public bool HasTerrain { get; private set; }
    public bool IsLoading { get; private set; } = false;

    private bool HideChunks = true;
    private float ViewDistance = 0;

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

    [Header("Prefabs")]
    public GameObject GolfHoleFlagPrefab;
    public GameObject GolfHoleBeaconPrefab;
    private LinePreview NextHoleBeacon;

    [Space]
    public TerrainData CurrentLoadedTerrain;

    [Space]
    public GolfBall GolfBall;

    public UnityAction OnHoleCompleted;




    private void Awake()
    {
        OnHoleCompleted += Utils.EMPTY;
    }

    private void OnDestroy()
    {
        OnHoleCompleted -= Utils.EMPTY;
        Clear();
    }

    public void Clear()
    {
        TerrainChunkManager.Clear();
        HasTerrain = false;
        CurrentLoadedTerrain = null;
    }

    public void Set(bool hideChunks, float viewDistance)
    {
        HideChunks = hideChunks;
        ViewDistance = viewDistance;
    }

    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data)
    {
        StartCoroutine(LoadTerrainAsync(data));
    }

    private void LateUpdate()
    {
        if (HideChunks && GolfBall != null && ViewDistance > 0)
        {
            foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
            {
                // Only set the chunks within render distance to be visible
                chunk.SetVisible((chunk.Bounds.center - GolfBall.transform.position).sqrMagnitude <= ViewDistance * ViewDistance);
            }
        }
    }

    private IEnumerator LoadTerrainAsync(TerrainData data)
    {
        DateTime before = DateTime.Now;
        Clear();
        IsLoading = true;

        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            // Instantiate the terrain
            TerrainChunk c = TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);

            // And instantiate all objects
            foreach (WorldObjectData worldObjectData in chunk.WorldObjects)
            {
                foreach ((Vector3, Vector3) worldPosition in worldObjectData.WorldPositions)
                {
                    Instantiate(worldObjectData.Prefab, worldPosition.Item1, Quaternion.Euler(worldPosition.Item2), c.transform);
                }
            }

            // Wait for next frame
            yield return null;
        }

        data.Courses.Sort((x,y) => x.Start.sqrMagnitude.CompareTo(y.Start.sqrMagnitude));

        // Assign hole numbers
        for (int i = 0; i < data.Courses.Count; i++)
        {
            data.Courses[i].Number = i;
        }

        // Assign the terrain at the end
        HasTerrain = true;
        CurrentLoadedTerrain = data;
        IsLoading = false;

        // Debug
        string message = "* Loaded terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0")
            + " seconds with " + data.Chunks.Count + " chunks and " + data.Courses.Count + " holes.";

        Debug.Log(message);

    }


    private void FixedUpdate()
    {
        if (CurrentLoadedTerrain != null)
        {
            // Current course
            if (GetCourse(GolfBall.Progress.CurrentCourse, out CourseData target))
            {
                // Ball was potted this frame
                if (target.BallWasPotted(GolfBall.Mask))
                {
                    GolfBall.HoleReached(target.Number, DateTime.Now);
                    int nextHole = target.Number + 1;

                    // Set the next hole now 
                    if (nextHole < CurrentLoadedTerrain.Courses.Count)
                    {
                        GetCourse(nextHole, out target);

                        // Respawn the ball here
                        SpawnGolfBall(target);

                        // Call the event once the ball has been respawned
                        OnHoleCompleted.Invoke();
                    }
                    else
                    {
                        Debug.LogError("No more courses left");
                    }
                }

                // Ensure the beacon is always active
                if (NextHoleBeacon == null)
                {
                    GameObject beaconObject = Instantiate(GolfHoleBeaconPrefab, transform);
                    beaconObject.name = "Next hole beacon";
                    NextHoleBeacon = beaconObject.GetComponent<LinePreview>();
                }

                // Set the position
                NextHoleBeacon.transform.position = target.Hole;

                // Calculate the beacon width
                float distanceSqr = (target.Hole - GolfBall.Position).sqrMagnitude;
                float maximumDistance = TerrainChunkManager.ChunkSizeWorldUnits * 2;
                float percent = Mathf.Clamp(distanceSqr / (maximumDistance * maximumDistance), 0f, 1f);

                // Set the width
                NextHoleBeacon.UpdateLineWidth(Mathf.Lerp(0.05f, 10, percent));


                // Set the points
                NextHoleBeacon.SetPoints(target.Hole, UP);
            }
        }
    }

    public bool GetCourse(int number, out CourseData hole)
    {
        if (number >= 0 && number < CurrentLoadedTerrain.Courses.Count)
        {
            hole = CurrentLoadedTerrain.Courses[number];
            return true;
        }

        hole = null;
        return false;
    }

    public void Restart()
    {
        if (GetCourse(0, out CourseData start))
        {
            GolfBall.Progress.Clear();
            SpawnGolfBall(start);
        }
        else
        {
            Debug.LogError("Could not respawn GolfBall as there is no first Hole.");
        }
    }

    public void UndoShot()
    {
        if (GolfBall.Progress.ShotsForThisHole > 0)
        {
            GolfBall.Stats.Shot s = GolfBall.Progress.ShotsCurrentCourse.Peek();

            MoveGolfBallAndWaitForNextShot(s.PositionFrom);

            GolfBall.SetValues(s.Rotation, s.Angle, s.Power);
        }
    }

    public void SpawnGolfBall(CourseData hole)
    {
        Vector3 spawnPoint = hole.Start;
        if (GroundCheck.DoRaycastDown(hole.Start + (UP * 25), out RaycastHit hit, 50))
        {
            spawnPoint = hit.point;
            Debug.Log("SUCCESS");
        }
        else
        {
            Debug.Log("FAILED");
        }

        // And move the ball there
        MoveGolfBallAndWaitForNextShot(spawnPoint + (UP * GolfBall.Radius));
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


    private void OnDrawGizmosSelected()
    {
        if (CurrentLoadedTerrain != null)
        {
            System.Random r = new System.Random(0);
            // Calculate the holes
            foreach (CourseData h in CurrentLoadedTerrain.Courses)
            {
                Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
                Gizmos.color = c;
                Gizmos.DrawLine(h.Start, h.Start + Vector3.up * 100);
                Gizmos.DrawLine(h.Hole, h.Hole + Vector3.up * 500);
            }
        }

    }

}
