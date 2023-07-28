using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, IManager
{
    public static UnityEvent<TerrainGenerator.GenerationSettings, bool> OnRequestStartGenerating = new UnityEvent<TerrainGenerator.GenerationSettings, bool>();

    [Header("Managers")]
    public TerrainGenerator TerrainGenerator;
    public TerrainManager TerrainManager;

    private HUD HUD;

    [Header("Golf Ball")]
    public ShotPreview GolfBallShotPreview;

    [Space]
    public List<GameObject> ObjectsToDisableWhileLoading;

    [Header("Cameras")]
    public CinemachineBrain MainCameraBrain;
    public Animator CameraStates;

    [Space]
    public CinemachineSmoothPath CoursePreviewDollyPath;
    public CinemachineDollyCart CoursePreviewDollyCart;
    public AnimationCurve CoursePreviewSpeedCurve = new AnimationCurve();

    private const string CameraSqrMagToTargetFloat = "SqrMagToTarget";
    private const string CameraCoursePreviewTriggerStart = "OnTriggerCoursePreviewStart", CameraCoursePreviewTriggerEnd = "OnTriggerCoursePreviewEnd";
    private const string CameraAiming = "IsAiming", CameraRolling = "IsRolling", CameraFlying = "IsFlying";

    [Space]
    public Camera MapCamera;

    [Header("Terrain settings")]
    private Gamerules Gamerule;
    private static readonly Gamerules Testing = new Gamerules(false, false, false, false);
    private static readonly Gamerules FixedArea = new Gamerules(true, true, true, true);

    public delegate void CourseGenerated(TerrainData data);
    public delegate void PreviewGenerated(Texture2D map);

    [Space]
    public Material Skybox;


    public const float DefaultMapCameraZoom = 500;

    public const float CoursePreviewDurationSeconds =10.0f;



    private void Awake()
    {
        OnRequestStartGenerating.AddListener(StartGeneration);

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        RenderSettings.skybox = Skybox;

        TerrainManager.OnCourseStarted += CourseStarted;
    }

    private void OnDestroy()
    {
        TerrainManager.OnCourseStarted -= CourseStarted;
    }

    private void Start()
    {
        Clear();

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(false);
        }



    }

    private void CourseStarted(CourseData data)
    {
        // Update the course preview dolly path
        CoursePreviewDollyPath.m_Waypoints = new CinemachineSmoothPath.Waypoint[] 
        { 
            new CinemachineSmoothPath.Waypoint() { position = data.Hole }, 
            new CinemachineSmoothPath.Waypoint() { position = data.Start }
        };

        // Update map camera
        /*
        Vector3 pos = (data.Start + data.Hole) / 2;
        pos.y = MapCamera.transform.position.y;
        MapCamera.transform.position = pos;
        MapCamera.orthographicSize = DefaultMapCameraZoom;
        MapCamera.enabled = false;
        */


        StartCoroutine(DoCoursePreview());
    }

    private IEnumerator DoCoursePreview()
    {
        Debug.Log("HERE");

        CameraStates.SetTrigger(CameraCoursePreviewTriggerStart);

        CoursePreviewDollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;

        for (float totalTime = 0; totalTime < CoursePreviewDurationSeconds; totalTime += Time.deltaTime)
        {
            CoursePreviewDollyCart.m_Position = CoursePreviewSpeedCurve.Evaluate(totalTime / CoursePreviewDurationSeconds);
            yield return null;
        }

        CameraStates.SetTrigger(CameraCoursePreviewTriggerEnd);

    }

    public void SetVisible(bool visible)
    {
        TerrainManager.SetVisible(visible);
        TerrainGenerator.SetVisible(visible);
    }

    public void RestartGame()
    {
        if (Gamerule.UseGolfBall)
        {
            TerrainManager.Restart();
        }
        if (Gamerule.UseHUD)
        {
            UpdateHUDShotCounter();
        }

        Logger.Log("Game has started. There are " + TerrainManager.CurrentLoadedTerrain.Courses.Count + " courses.");
    }

    public void StartGeneration(TerrainGenerator.GenerationSettings settings, bool testing)
    {
        if (TerrainGenerator.IsGenerating) return;

        LoadingScreen.Instance.SetVisible(true);

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(false);
        }
        SetVisible(false);

        Clear();

        Logger.OnLogMessage.AddListener(message => { LoadingScreen.Instance.Info.text = message; });

        // Set the game rules
        if (testing)
        {
            Gamerule = Testing;

        }
        else
        {
            Gamerule = FixedArea;

        }

        // Now generate the terrain
        TerrainGenerator.Generate(settings, LoadTerrain);
    }

    private void LoadTerrain(TerrainData data)
    {
        if (data.Courses.Count == 0)
        {
            Debug.LogError("NO GOLF COURSES!");

            LoadingScreen.Instance.SetVisible(false);
            HUD.QuitToMenuPressed();

            MainMenu.Instance.InvalidSeedText.SetActive(true);
        }
        else
        {
            MainMenu.Instance.InvalidSeedText.SetActive(false);

            StartCoroutine(WaitUntilGameLoaded(data));
        }
    }

    private IEnumerator WaitUntilGameLoaded(TerrainData data)
    {
        // Set up the TerrainManager
        TerrainManager.LoadTerrain(data);

        // Ensure there is terrain before we start
        while (TerrainManager.IsLoading) yield return null;

        // Sanity check
        if (!TerrainManager.HasTerrain)
        {
            Debug.LogError("Terrain manager does not have terrain");
            yield break;
        }

        if (Gamerule.UseGolfBall)
        {
            TerrainManager.GolfBall.OnOutOfBounds += TerrainManager.UndoShot;
        }

        LoadingScreen.Instance.SetVisible(false);

        if(MainMenu.Instance)
        {
            MainMenu.Instance.SetVisible(false);
        }

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(true);
        }
        SetVisible(true);

        // Disable the golf ball if we dont need it
        TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);

        if (Gamerule.UseHUD)
        {
            HUD.Compass.Following = TerrainManager.GolfBall.transform;
            HUD.SetVisible(true);
        }

        // Start the game
        RestartGame();
    }



    private void UpdateLoop()
    {
        if(Gamerule.UseViewDistance)
        {
            TerrainManager.UpdateLOD(MainCameraBrain.OutputCamera.transform.position, TerrainManager.GolfBall.Position);
        }

        if (Gamerule.UseGolfBall && TerrainManager.HasTerrain)
        {
            // Taking a shot
            bool isAiming = TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming;

            // Set the preview active or not
            GolfBallShotPreview.gameObject.SetActive(isAiming);

            if (Gamerule.UseHUD && HUD != null)
            {
                HUD.ShootingMenu.SetActive(isAiming);

                // TODO
                HUD.Minimap.SetActive(false);

                // Update the camera angles
                switch (TerrainManager.GolfBall.State)
                {
                    // Shooting mode
                    case GolfBall.PlayState.Aiming:

                        const int heldButtonMultiplier = 24, touchMultiplier = 10;

                        // Calculate the deltas for each 
                        Vector2 rotationAndAngleDelta = Time.deltaTime * touchMultiplier * HUD.MainSlider.DeltaPosition;
                        Vector2 powerDelta = Time.deltaTime * touchMultiplier * HUD.PowerSlider.DeltaPosition;

                        // Make sure power has priority over rotation and angle
                        if (powerDelta.x != 0 || powerDelta.y != 0)
                        {
                            rotationAndAngleDelta = Vector2.zero;
                        }

                        float rotationDelta = rotationAndAngleDelta.x, angleDelta = rotationAndAngleDelta.y;

                        // Rotation
                        // Move less
                        if (HUD.RotationLess.IsPressed && !HUD.RotationMore.IsPressed)
                        {
                            rotationDelta = -heldButtonMultiplier * Time.deltaTime;
                        }
                        // Move more
                        else if (!HUD.RotationLess.IsPressed && HUD.RotationMore.IsPressed)
                        {
                            rotationDelta = heldButtonMultiplier * Time.deltaTime;
                        }

                        // Angle
                        // Move less
                        if (HUD.AngleLess.IsPressed && !HUD.AngleMore.IsPressed)
                        {
                            angleDelta = -heldButtonMultiplier * Time.deltaTime;
                        }
                        // Move more
                        else if (!HUD.AngleLess.IsPressed && HUD.AngleMore.IsPressed)
                        {
                            angleDelta = heldButtonMultiplier * Time.deltaTime;
                        }


                        // Set the new values
                        TerrainManager.GolfBall.SetValues(TerrainManager.GolfBall.Rotation + rotationDelta, TerrainManager.GolfBall.Angle + angleDelta, TerrainManager.GolfBall.Power + (powerDelta.y / 50f));

                        // Update the shot preview
                        Vector3[] positions = TerrainManager.GolfBall.CalculateShotPreviewWorldPositions(1000, 0.1f).ToArray();
                        GolfBallShotPreview.UpdateShotPreview(TerrainManager.GolfBall.Angle.ToString("0") + "°", TerrainManager.GolfBall.Angle, positions, TerrainManager.GolfBall.transform.rotation);


                        // Update the HUD to display the correct values
                        HUD.PowerSlider.DisplayValue.text = (TerrainManager.GolfBall.Power * 100).ToString("0") + "%";

                        // Set the power slider colour
                        Color p = HUD.PowerSlider.Gradient.Evaluate(TerrainManager.GolfBall.Power);
                        p.a = HUD.PowerSliderBackgroundAlpha;
                        HUD.PowerSlider.Background.color = p;

                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, -1);
                        break;
                    // Flying mode
                    case GolfBall.PlayState.Flying:

                        float sqrMag = (GolfBallShotPreview.ShotPreviewTarget.position - TerrainManager.GolfBall.transform.position).sqrMagnitude;
                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, sqrMag);
                        break;
                    // Rolling mode
                    case GolfBall.PlayState.Rolling:
                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, -1);
                        break;
                }

                CameraStates.SetBool(CameraAiming, TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming);
                CameraStates.SetBool(CameraFlying, TerrainManager.GolfBall.State == GolfBall.PlayState.Flying);
                CameraStates.SetBool(CameraRolling, TerrainManager.GolfBall.State == GolfBall.PlayState.Rolling);

            }
        }
    }

    private void Update()
    {
        UpdateLoop();
    }

    public void Clear()
    {
        TerrainManager.Clear();
        TerrainGenerator.Clear();

        if (HUD) HUD.Clear();
    }

    public void SetHUD(HUD instance)
    {
        HUD = instance;

        HUD.OnShootPressed.AddListener(TerrainManager.GolfBall.Shoot);
        HUD.OnShootPressed.AddListener(UpdateHUDShotCounter);

        TerrainManager.OnCourseCompleted += UpdateHUDShotCounter;
    }

    private void UpdateHUDShotCounter<T>(T t)
    {
        UpdateHUDShotCounter();
    }

    private void UpdateHUDShotCounter()
    {
        if (HUD != null)
        {
            // Update the shots counter
            HUD.Shots.text = TerrainManager.GolfBall.Progress.ShotsForThisHole.ToString();


            GolfBall.Stats.Pot[] holes = TerrainManager.GolfBall.Progress.CoursesCompleted.ToArray();

            if (holes != null && holes.Length > 0)
            {
                // Add rows until we have enough
                while (HUD.ScoreboardRows.Count < holes.Length - 1)
                {
                    GameObject g = Instantiate(HUD.ScoreRowPrefab, HUD.ScoreRowParent.transform);
                    ScoreboardRow row = g.GetComponent<ScoreboardRow>();

                    HUD.ScoreboardRows.Add(row);
                }

                // Update each ones data
                for (int holeIndex = 0; holeIndex < holes.Length - 1; holeIndex++)
                {
                    int rowIndex = HUD.ScoreboardRows.Count - 1 - holeIndex;

                    HUD.ScoreboardRows[rowIndex].HoleNumber.text = "#" + holes[holeIndex].CourseNumber;
                    HUD.ScoreboardRows[rowIndex].Shots.text = holes[holeIndex].ShotsTaken.ToString();


                    TimeSpan time = holes[holeIndex].TimeReached - holes[holeIndex + 1].TimeReached;

                    // Set the time message 
                    string timeMessage;
                    if (time.Minutes > 0)
                    {
                        timeMessage = time.Minutes + "m " + (time.TotalSeconds % 60).ToString("0.0") + "s";
                    }
                    else
                    {
                        timeMessage = time.TotalSeconds.ToString("0.0") + "s";
                    }

                    HUD.ScoreboardRows[rowIndex].Time.text = timeMessage;
                }
            }
        }
    }

    public struct Gamerules
    {
        public bool UseViewDistance;
        public bool UseMeshLOD;
        public bool UseHUD;
        public bool UseGolfBall;

        public Gamerules(bool useViewDistance, bool useMeshLOD, bool HUD, bool ball)
        {
            UseViewDistance = useViewDistance;
            UseMeshLOD = useMeshLOD;

            UseHUD = HUD;
            UseGolfBall = ball;
        }
    }

    public enum TerrainGenerationMethod
    {
        FixedArea,
        Testing
    }
}
