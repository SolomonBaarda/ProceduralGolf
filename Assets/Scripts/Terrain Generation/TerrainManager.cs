using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TerrainManager : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    [Header("References")]
    public TerrainChunkManager TerrainChunkManager;
    public GolfBall GolfBall;
    public LinePreview NextHoleBeacon;
    public GameObject NextHoleFlag;

    public TerrainData CurrentLoadedTerrain { get; private set; }
    public bool HasTerrain { get; private set; } = false;
    public bool IsLoading { get; private set; } = false;

    private bool HideChunks = true;
    private float ViewDistance = 0;

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

    [Header("Events")]
    public UnityAction<CourseData> OnCourseStarted;
    public UnityAction<CourseData> OnCourseCompleted;

    private void Awake()
    {
        OnCourseStarted += StartCourse;
        OnCourseCompleted += CourseCompleted;
    }

    private void OnDestroy()
    {
        OnCourseStarted -= StartCourse;
        OnCourseCompleted -= CourseCompleted;
        Clear();
    }

    private void CourseCompleted(CourseData course)
    {
        GolfBall.HoleReached(course.Number, DateTime.Now);

        // Get the next course if there is one
        if (GetCourse(course.Number + 1, out CourseData next))
        {
            OnCourseStarted.Invoke(next);
        }
        else
        {
            Debug.LogError("No more courses left");
        }
    }

    private void StartCourse(CourseData course)
    {
        // Update course game object positions
        NextHoleFlag.transform.position = course.Hole;
        NextHoleBeacon.SetPoints(new Vector3[] { course.Hole, course.Hole + UP * 100 });

        SpawnGolfBall(course);
    }

    public void Clear()
    {
        TerrainChunkManager.Clear();
        HasTerrain = false;
        CurrentLoadedTerrain = null;
        IsLoading = false;
    }

    public void Set(bool hideChunks, float viewDistance)
    {
        HideChunks = hideChunks;
        ViewDistance = viewDistance;
    }

    /// <summary>
    /// Load the TerrainData into the TerrainChunkManager.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data)
    {
        StartCoroutine(LoadTerrainAsync(data));
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

        data.Courses.Sort((x, y) => x.Start.sqrMagnitude.CompareTo(y.Start.sqrMagnitude));

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

        Logger.Log(message);
    }

    private void FixedUpdate()
    {
        if (CurrentLoadedTerrain != null && GetCourse(GolfBall.Progress.CurrentCourse, out CourseData target))
        {
            // Ball was potted this frame
            if (target.BallWasPotted(GolfBall.Mask))
            {
                OnCourseCompleted.Invoke(target);
            }

            // Update the next hole beacon width
            float distanceSqr = (target.Hole - GolfBall.Position).sqrMagnitude;
            const float maximumDistance = TerrainChunkManager.ChunkSizeWorldUnits * 2;
            float percent = Mathf.Clamp01(distanceSqr / (maximumDistance * maximumDistance));
            NextHoleBeacon.UpdateLineWidth(Mathf.Lerp(0.05f, 10, percent));
        }
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
            OnCourseStarted.Invoke(start);
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
            Logger.Log("SUCCESSFULLY SPAWNED GOLF BALL PROPERLY");
        }
        else
        {
            Logger.Log("FAILED TO SPAWN GOLF BALL PROPERLY");
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
            // Calculate the holes
            foreach (CourseData h in CurrentLoadedTerrain.Courses)
            {
                Gizmos.color = h.Colour;

                Gizmos.DrawLine(h.Start, h.Start + Vector3.up * 100);
                Gizmos.DrawLine(h.Hole, h.Hole + Vector3.up * 200);
            }
        }
    }

}
