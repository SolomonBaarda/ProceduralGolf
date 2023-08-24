using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TerrainManager : MonoBehaviour, IManager
{
    [Header("References")]
    public TerrainChunkManager TerrainChunkManager;
    public MinimapManager MinimapManager;
    public CameraManager CameraManager;
    public GolfBall GolfBall;

    public MeshFilter WaterMesh;
    public MeshRenderer WaterRenderer;
    public MeshFilter WaterMeshMinimap;


    public Transform NextHolePosition;

    public TerrainData CurrentLoadedTerrain { get; private set; }
    public bool HasTerrain { get; private set; } = false;
    public bool IsLoading { get; private set; } = false;

    [SerializeField]
    private List<float> LODViewSettings = new List<float>();


    [Header("Events")]
    public UnityAction<CourseData> OnCourseStarted;
    public UnityAction<CourseData> OnCourseCompleted;
    public UnityAction<GolfBall.Stats.Pot[]> OnGameOver;

    [Header("Fireworks")]
    public Transform FireworkParent;
    public float FireworkHeight = 20.0f;
    public float FireworkRadiusVariance = 10.0f;
    [Min(2)]
    public float FireworkDisplaySeconds = 5.0f;
    public int MaxNumConcurrentFireworks = 20;
    public int FireworkChancePerFrame = 5;

    public List<GameObject> FireworkPrefabs = new List<GameObject>();


    private void Awake()
    {
        OnCourseStarted += StartCourse;
        OnCourseCompleted += CourseCompleted;
    }

    private void OnDestroy()
    {
        OnCourseStarted -= StartCourse;
        OnCourseCompleted -= CourseCompleted;
        Reset();
    }

    private void CourseCompleted(CourseData course)
    {
        GolfBall.HoleReached(course.Number, DateTime.Now);
        Debug.Log($"Course #{course.Number} completed");

        StartCoroutine(PlayFireworksOnCourseEnd(course));
    }

    private IEnumerator PlayFireworksOnCourseEnd(CourseData course)
    {
        CameraManager.DoCourseEndPreview(1.0f, FireworkDisplaySeconds - 2, 1.0f);

        Vector3 fireworkCentre = course.Hole + Vector3.up * FireworkHeight;

        if (FireworkPrefabs.Count == 0)
        {
            Debug.LogError("No firework prefabs have been set");
            yield break;
        }

        for (float t = 0; t < FireworkDisplaySeconds; t += Time.deltaTime)
        {
            TrySpawnRandomFirework(fireworkCentre);

            yield return null;
        }

        // Get the next course if there is one
        if (GetCourse(course.Number + 1, out CourseData next))
        {
            OnCourseStarted.Invoke(next);
        }
        else
        {
            OnGameOver.Invoke(GolfBall.Progress.CoursesCompleted.ToArray());
        }
    }

    private void TrySpawnRandomFirework(Vector3 centre)
    {
        // Ensure we don't have too many fireworks at the same time
        if(FireworkParent.childCount < MaxNumConcurrentFireworks)
        {
            if(UnityEngine.Random.Range(0, FireworkChancePerFrame) == 0)
            {
                GameObject prefab = FireworkPrefabs[UnityEngine.Random.Range(0, FireworkPrefabs.Count)];
                Vector3 randomPosition = centre + (UnityEngine.Random.insideUnitSphere * FireworkRadiusVariance);
                Quaternion randomRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0);

                Instantiate(prefab, randomPosition, randomRotation, FireworkParent);
            }
        }
    }

    private void StartCourse(CourseData course)
    {
        // Update course game object positions
        NextHolePosition.position = course.Hole;

        // Calculate the facing direction based off the course preview camera path
        var facingHoleDirection = Quaternion.LookRotation(course.PathStartToEnd[1] - course.PathStartToEnd[0], Vector3.up);

        // And move the ball there
        GolfBall.MoveGolfBallAndWaitForNextShot(course.Start + (Vector3.up * GolfBall.Radius), facingHoleDirection.eulerAngles.y);

        // Now update the minimap since everything should be in position
        MinimapManager.UpdateMinimapForCourse(course.Hole, course.PathStartToEnd);
    }

    public void Reset()
    {
        TerrainChunkManager.Reset();

        HasTerrain = false;
        CurrentLoadedTerrain = null;
        IsLoading = false;
    }

    public void SetVisible(bool visible)
    {
        TerrainChunkManager.SetVisible(visible);

        GolfBall.gameObject.SetActive(visible);
        NextHolePosition.gameObject.SetActive(visible);
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
        Reset();
        IsLoading = true;
       
        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            // Instantiate the terrain
            TerrainChunk c = TerrainChunkManager.TryAddChunk(chunk);

            // And instantiate all objects
            foreach (WorldObjectData worldObjectData in chunk.WorldObjects)
            {
                foreach ((Vector3, Vector3) worldPosition in worldObjectData.WorldPositionsAndRotations)
                {
                    Instantiate(worldObjectData.Prefab, worldPosition.Item1, Quaternion.Euler(worldPosition.Item2), c.transform);
                }
            }

            // Wait for next frame
            yield return null;
        }

        // Assign hole numbers
        for (int i = 0; i < data.Courses.Count; i++)
        {
            data.Courses[i].Number = i;
        }

        GolfBall.InvalidBiomesForCurrentCourse = data.InvalidBiomesForCurrentCourse;


        // Assign water mesh
        WaterMesh.gameObject.SetActive(data.DoWater);
        WaterMeshMinimap.gameObject.SetActive(data.DoWater);

        WaterMesh.mesh = data.WaterMesh;
        WaterMeshMinimap.mesh = data.WaterMesh;

        Vector3 waterPos = WaterMesh.transform.position;
        waterPos.y = data.WaterMeshHeight;
        WaterMesh.transform.position = waterPos;
        WaterMeshMinimap.transform.position = waterPos;

        Material waterMaterial = WaterRenderer.material;
        waterMaterial.SetVector("_NormalTiling", new Vector2(data.WaterMeshTiling, data.WaterMeshTiling));


        // Assign the terrain at the end
        HasTerrain = true;
        CurrentLoadedTerrain = data;
        IsLoading = false;

        // Debug
        Debug.Log($"Loaded terrain in {(DateTime.Now - before).TotalSeconds:0.0} seconds with {data.Chunks.Count} chunks and {data.Courses.Count} courses.");

        string invalidBiomesString = "";
        foreach (var biome in data.InvalidBiomesForCurrentCourse)
        {
            invalidBiomesString += $"{biome}, ";
        }
        Debug.Log($"Invalid biomes for current course: {invalidBiomesString}");
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
        }
    }

    public void MakeAllChunksVisible()
    {
        foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
        {
            chunk.SetLODIndex(0, true);
        }
    }

    public void UpdateLOD(Vector3 currentCameraPositionm, Vector3 currentGolfBallPosition)
    {
        foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
        {
            Vector3 distanceFromCamera = currentCameraPositionm - chunk.Bounds.center;

            // Calculate which visual LOD we should be using
            float distanceSqrMag = Vector2.SqrMagnitude(new Vector2(distanceFromCamera.x, distanceFromCamera.z));

            int viewLOD = 0;
            foreach (float viewDistance in LODViewSettings)
            {
                distanceSqrMag -= viewDistance * viewDistance;

                if (distanceSqrMag <= 0)
                {
                    break;
                }

                viewLOD++;
            }


            Vector3 distanceFromBall = currentGolfBallPosition - chunk.Bounds.center;

            // Enable collisions for the 2x2 chunks surrounding the ball
            bool collisionsEnabled = Math.Abs(distanceFromBall.x) <= TerrainChunkData.ChunkSizeWorldUnits &&
                Math.Abs(distanceFromBall.z) <= TerrainChunkData.ChunkSizeWorldUnits;

            if (collisionsEnabled && viewLOD >= LODViewSettings.Count)
            {
                viewLOD = LODViewSettings.Count - 1;
            }

            // Only set the chunks within render distance to be visible
            chunk.SetLODIndex(viewLOD, collisionsEnabled);
        }
    }

    private bool GetCourse(int number, out CourseData hole)
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

            GolfBall.MoveGolfBallAndWaitForNextShot(s.PositionFrom, s.Rotation, s.Power, s.Angle);
        }
    }

    private void OnDrawGizmos()
    {
        if (CurrentLoadedTerrain != null)
        {
            // Calculate the holes
            foreach (CourseData c in CurrentLoadedTerrain.Courses)
            {
                Gizmos.color = c.Colour;
                Gizmos.DrawLine(c.Start, c.Start + (Vector3.up * 100));
                //Gizmos.DrawLine(c.Midpoint, c.Midpoint + (Vector3.up * 100));
                Gizmos.DrawLine(c.Hole, c.Hole + (Vector3.up * 200));
            }
        }
    }

}
