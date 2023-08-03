using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour, IManager
{
    public static UnityEvent<TerrainGenerator.GenerationSettings, bool> OnRequestStartGenerating = new UnityEvent<TerrainGenerator.GenerationSettings, bool>();

    [Header("Game State")]
    public GameState State;

    [Header("Managers")]
    public TerrainGenerator TerrainGenerator;
    [Space]
    public TerrainManager TerrainManager;
    public LoadingScreenManager LoadingScreenManager;
    public MainMenuManager MainMenuManager;
    public HUDManager HUDManager;
    public CameraManager CameraManager;

    [Header("Golf Ball")]
    public ShotPreview GolfBallShotPreview;

    [Space]
    public List<GameObject> ObjectsToDisableWhileLoading;

    [Header("Terrain settings")]
    private Gamerules Gamerule;
    private static readonly Gamerules Testing = new Gamerules(false, false, false, false);
    private static readonly Gamerules FixedArea = new Gamerules(true, true, true, true);

    public delegate void CourseGenerated(TerrainData data);
    public delegate void PreviewGenerated(Texture2D map);

    [Space]
    public Material Skybox;


    private void Awake()
    {
        RenderSettings.skybox = Skybox;

        OnRequestStartGenerating.AddListener(StartGeneration);


        TerrainManager.OnCourseStarted += OnStartCourse;
        TerrainManager.OnCourseCompleted += UpdateHUDShotCounter;

        MainMenuManager.OnPressStartGame.AddListener(StartGame);
        MainMenuManager.OnPressQuit.AddListener(QuitApplication);

        HUDManager.OnQuitToMenuPressed.AddListener(QuitToMainMenu);
        HUDManager.OnRestartPressed.AddListener(RestartGameFromFirstCourse);
        HUDManager.OnShootPressed.AddListener(TerrainManager.GolfBall.Shoot);
        HUDManager.OnShootPressed.AddListener(UpdateHUDShotCounter);

        Logger.OnLogMessage.AddListener(message => { LoadingScreenManager.Info.text = message; });

        TerrainManager.GolfBall.OnOutOfBounds += TerrainManager.UndoShot;

        QuitToMainMenu();
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.MainMenu:
                break;
            case GameState.GameLoading:
                break;
            case GameState.CoursePreview: 
                break;
            case GameState.InGame:
                DoGameLoop();
                break;
        }
    }

    private void QuitToMainMenu()
    {
        SetGameState(GameState.MainMenu);
    }

    private void StartGame(TerrainGenerator.GenerationSettings settings)
    {
        StartGeneration(settings, false);
    }

    private void SetGameState(GameState state)
    {
        State = state;

        switch (State)
        {
            case GameState.MainMenu:

                MainMenuManager.SetVisible(true);
                MainMenuManager.SetLoading(false);
                LoadingScreenManager.SetVisible(false);

                HUDManager.SetVisible(false);
                TerrainManager.SetVisible(false);

                SetVisible(false);

                // TODO TEST:
                //StopAllCoroutines();
                //CameraManager.StopAllCoroutines();
                //TerrainManager.GolfBall.StopAllCoroutines();
                //TerrainGenerator.StopAllCoroutines();

                break;
            case GameState.GameLoading:

                MainMenuManager.SetVisible(true);
                MainMenuManager.SetLoading(true);
                LoadingScreenManager.SetVisible(true);

                HUDManager.SetVisible(false);
                TerrainManager.SetVisible(false);

                SetVisible(false);

                break;
            case GameState.CoursePreview:

                MainMenuManager.SetVisible(false);
                MainMenuManager.SetLoading(false);
                LoadingScreenManager.SetVisible(false);
                HUDManager.SetVisible(false);

                TerrainManager.SetVisible(true);
                SetVisible(true);
                // Disable the golf ball if we dont need it
                TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);


                break;
            case GameState.InGame:

                MainMenuManager.SetVisible(false);
                MainMenuManager.SetLoading(false);
                LoadingScreenManager.SetVisible(false);

                TerrainManager.SetVisible(true);

                SetVisible(true);

                // Disable the golf ball if we dont need it
                TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);

                if (Gamerule.UseHUD)
                {
                    HUDManager.Compass.Following = TerrainManager.GolfBall.transform;
                }

                HUDManager.SetVisible(Gamerule.UseHUD);

                break;
        }
    }

    private void QuitApplication()
    {
        Application.Quit();
    }

    public void RestartGameFromFirstCourse()
    {
        TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);

        if (Gamerule.UseGolfBall)
        {
            TerrainManager.Restart();
        }

        if (Gamerule.UseHUD)
        {
            UpdateHUDShotCounter();
        }
    }

    private void OnStartCourse(CourseData data)
    {
        SetGameState(GameState.CoursePreview);

        // Update the course preview dolly path
        CinemachineSmoothPath.Waypoint[] path = data.PathStartToEnd
            .Reverse<Vector3>()
            .Select(x => new CinemachineSmoothPath.Waypoint() { position = x })
            .ToArray();

        CameraManager.StartCoursePreview(path, OnCoursePreviewCompleted);
    }

    private void OnCoursePreviewCompleted()
    {
        SetGameState(GameState.InGame);

        Logger.Log("Game has started. There are " + TerrainManager.CurrentLoadedTerrain.Courses.Count + " courses.");
    }



    public void SetVisible(bool visible)
    {
        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(visible);
        }
    }



    public void StartGeneration(TerrainGenerator.GenerationSettings settings, bool testing)
    {
        if (TerrainGenerator.IsGenerating || State == GameState.GameLoading) return;

        SetGameState(GameState.GameLoading);

        Reset();

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

            LoadingScreenManager.SetVisible(false);
            HUDManager.QuitToMenuPressed();

            MainMenuManager.InvalidSeedText.SetActive(true);
        }
        else
        {
            MainMenuManager.InvalidSeedText.SetActive(false);

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

        // Start the game
        RestartGameFromFirstCourse();
    }



    private void DoGameLoop()
    {
        if(Gamerule.UseViewDistance)
        {
            TerrainManager.UpdateLOD(CameraManager.MainCamera.transform.position, TerrainManager.GolfBall.Position);
        }

        if (Gamerule.UseGolfBall && TerrainManager.HasTerrain)
        {
            // Taking a shot
            bool isAiming = TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming;

            // Set the preview active or not
            GolfBallShotPreview.gameObject.SetActive(isAiming);

            if (Gamerule.UseHUD && HUDManager != null)
            {
                HUDManager.ShootingMenu.SetActive(isAiming);

                // TODO
                HUDManager.Minimap.SetActive(false);

                // Update the camera angles
                switch (TerrainManager.GolfBall.State)
                {
                    // Shooting mode
                    case GolfBall.PlayState.Aiming:

                        const int heldButtonMultiplier = 24, touchMultiplier = 10;

                        // Calculate the deltas for each 
                        Vector2 rotationAndAngleDelta = Time.deltaTime * touchMultiplier * HUDManager.MainSlider.DeltaPosition;
                        Vector2 powerDelta = Time.deltaTime * touchMultiplier * HUDManager.PowerSlider.DeltaPosition;

                        // Make sure power has priority over rotation and angle
                        if (powerDelta.x != 0 || powerDelta.y != 0)
                        {
                            rotationAndAngleDelta = Vector2.zero;
                        }

                        float rotationDelta = rotationAndAngleDelta.x, angleDelta = rotationAndAngleDelta.y;

                        // Rotation
                        // Move less
                        if (HUDManager.RotationLess.IsPressed && !HUDManager.RotationMore.IsPressed)
                        {
                            rotationDelta = -heldButtonMultiplier * Time.deltaTime;
                        }
                        // Move more
                        else if (!HUDManager.RotationLess.IsPressed && HUDManager.RotationMore.IsPressed)
                        {
                            rotationDelta = heldButtonMultiplier * Time.deltaTime;
                        }

                        // Angle
                        // Move less
                        if (HUDManager.AngleLess.IsPressed && !HUDManager.AngleMore.IsPressed)
                        {
                            angleDelta = -heldButtonMultiplier * Time.deltaTime;
                        }
                        // Move more
                        else if (!HUDManager.AngleLess.IsPressed && HUDManager.AngleMore.IsPressed)
                        {
                            angleDelta = heldButtonMultiplier * Time.deltaTime;
                        }


                        // Set the new values
                        TerrainManager.GolfBall.SetValues(TerrainManager.GolfBall.Rotation + rotationDelta, TerrainManager.GolfBall.Angle + angleDelta, TerrainManager.GolfBall.Power + (powerDelta.y / 50f));

                        // Update the shot preview
                        Vector3[] positions = TerrainManager.GolfBall.CalculateShotPreviewWorldPositions(1000, 0.1f).ToArray();
                        GolfBallShotPreview.UpdateShotPreview(TerrainManager.GolfBall.Angle.ToString("0") + "°", TerrainManager.GolfBall.Angle, positions, TerrainManager.GolfBall.transform.rotation);


                        // Update the HUD to display the correct values
                        HUDManager.PowerSlider.DisplayValue.text = (TerrainManager.GolfBall.Power * 100).ToString("0") + "%";

                        // Set the power slider colour
                        Color p = HUDManager.PowerSlider.Gradient.Evaluate(TerrainManager.GolfBall.Power);
                        p.a = HUDManager.PowerSliderBackgroundAlpha;
                        HUDManager.PowerSlider.Background.color = p;

                        CameraManager.SetGolfBallSquareMagnitudeToTarget(-1f);
                        break;
                    // Flying mode
                    case GolfBall.PlayState.Flying:

                        float sqrMag = (GolfBallShotPreview.ShotPreviewTarget.position - TerrainManager.GolfBall.transform.position).sqrMagnitude;
                        CameraManager.SetGolfBallSquareMagnitudeToTarget(sqrMag);
                        break;
                    // Rolling mode
                    case GolfBall.PlayState.Rolling:
                        CameraManager.SetGolfBallSquareMagnitudeToTarget(-1f);
                        break;
                }
            }
        }
    }



    public void Reset()
    {
        TerrainManager.Reset();

        if (HUDManager) HUDManager.Reset();
    }

    private void UpdateHUDShotCounter<T>(T t)
    {
        UpdateHUDShotCounter();
    }

    private void UpdateHUDShotCounter()
    {
        if (HUDManager != null)
        {
            // Update the shots counter
            HUDManager.Shots.text = TerrainManager.GolfBall.Progress.ShotsForThisHole.ToString();


            GolfBall.Stats.Pot[] holes = TerrainManager.GolfBall.Progress.CoursesCompleted.ToArray();

            if (holes != null && holes.Length > 0)
            {
                // Add rows until we have enough
                while (HUDManager.ScoreboardRows.Count < holes.Length - 1)
                {
                    GameObject g = Instantiate(HUDManager.ScoreRowPrefab, HUDManager.ScoreRowParent.transform);
                    ScoreboardRow row = g.GetComponent<ScoreboardRow>();

                    HUDManager.ScoreboardRows.Add(row);
                }

                // Update each ones data
                for (int holeIndex = 0; holeIndex < holes.Length - 1; holeIndex++)
                {
                    int rowIndex = HUDManager.ScoreboardRows.Count - 1 - holeIndex;

                    HUDManager.ScoreboardRows[rowIndex].HoleNumber.text = "#" + holes[holeIndex].CourseNumber;
                    HUDManager.ScoreboardRows[rowIndex].Shots.text = holes[holeIndex].ShotsTaken.ToString();


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

                    HUDManager.ScoreboardRows[rowIndex].Time.text = timeMessage;
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

    public enum GameState
    {
        MainMenu = 0,
        GameLoading = 1,
        CoursePreview = 2,
        InGame = 3
    }
}
