using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

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
    public MinimapManager MinimapManager;

    [Header("Golf Ball")]
    public ShotPreview GolfBallShotPreview;

    [Space]
    public List<GameObject> ObjectsToDisableWhileLoading;

    [Header("Terrain settings")]
    private Gamerules Gamerule;
    private static readonly Gamerules Testing = new Gamerules(false, false, false, false);
    private static readonly Gamerules FixedArea = new Gamerules(true, true, true, true);

    [Space]
    public Material Skybox;


    private void Awake()
    {
        RenderSettings.skybox = Skybox;

        OnRequestStartGenerating.AddListener(StartGeneration);


        TerrainManager.OnCourseStarted += OnStartCourse;
        TerrainManager.OnCourseCompleted += (x) => { SetGameState(GameState.CourseEnd); UpdateHUDCourseProgressScreen(); UpdateHUDShotCounter(); };
        TerrainManager.OnGameOver += OnGameOver;

        MainMenuManager.OnPressStartGame.AddListener(StartGame);
        MainMenuManager.OnPressQuit.AddListener(QuitApplication);

        HUDManager.OnQuitToMenuPressed.AddListener(QuitToMainMenu);
        HUDManager.OnRestartPressed.AddListener(RestartGameFromFirstCourse);
        HUDManager.OnShootPressed.AddListener(TerrainManager.GolfBall.Shoot);
        HUDManager.OnShootPressed.AddListener(UpdateHUDShotCounter);

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
                UpdateLOD();
                break;
            case GameState.InGame:
                UpdateLOD();
                DoGameLoop();
                break;
            case GameState.CourseEnd:
                UpdateLOD();
                break;
        }
    }

    private void QuitToMainMenu()
    {
        StopAllCoroutines();

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
                CameraManager.StartMainMenu();

                SetVisible(false);

                break;
            case GameState.GameLoading:

                MainMenuManager.SetVisible(true);
                MainMenuManager.SetLoading(true);
                LoadingScreenManager.SetVisible(true);

                HUDManager.SetVisible(false);
                TerrainManager.SetVisible(false);
                CameraManager.StartMainMenu();

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
                GolfBallShotPreview.gameObject.SetActive(false);

                break;
            case GameState.InGame:

                MainMenuManager.SetVisible(false);
                MainMenuManager.SetLoading(false);
                LoadingScreenManager.SetVisible(false);

                TerrainManager.SetVisible(true);
                CameraManager.StartGameCameras();

                SetVisible(true);

                // Disable the golf ball if we dont need it
                TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);
                GolfBallShotPreview.gameObject.SetActive(Gamerule.UseGolfBall);

                if (Gamerule.UseHUD)
                {
                    HUDManager.Compass.Following = TerrainManager.GolfBall.transform;
                }

                HUDManager.SetVisible(Gamerule.UseHUD);

                break;
            case GameState.CourseEnd:

                MainMenuManager.SetVisible(false);
                MainMenuManager.SetLoading(false);
                LoadingScreenManager.SetVisible(false);
                HUDManager.SetVisible(false);

                TerrainManager.SetVisible(true);
                SetVisible(true);

                // Disable the golf ball if we dont need it
                TerrainManager.GolfBall.gameObject.SetActive(Gamerule.UseGolfBall);
                GolfBallShotPreview.gameObject.SetActive(false);

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
        else
        {
            SetGameState(GameState.InGame);
        }

        if (Gamerule.UseHUD)
        {
            UpdateHUDCourseProgressScreen();
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
        TerrainManager.GolfBall.Progress.TimeStartedCurrentCourse = DateTime.Now;

        Debug.Log($"Game has started. There are {TerrainManager.CurrentLoadedTerrain.Courses.Count} courses.");
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
        if (data.Courses.Count == 0 && Gamerule.UseGolfBall)
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

        MinimapManager.MinimapCamera.backgroundColor = data.BackgroundColour;

        if (!Gamerule.UseViewDistance)
        {
            TerrainManager.MakeAllChunksVisible();
        }

        // Start the game
        RestartGameFromFirstCourse();
    }


    private void UpdateLOD()
    {
        if (Gamerule.UseViewDistance)
        {
            TerrainManager.UpdateLOD(CameraManager.MainCamera.transform.position, TerrainManager.GolfBall.Position);
        }
    }

    private void DoGameLoop()
    {
        if (Gamerule.UseGolfBall && TerrainManager.HasTerrain)
        {
            // Taking a shot
            bool isAiming = TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming;

            // Set the preview active or not
            GolfBallShotPreview.gameObject.SetActive(isAiming);

            if (Gamerule.UseHUD && HUDManager != null)
            {
                HUDManager.ShootingCanvas.SetActive(isAiming);

                CameraManager.SetGolfBallYVelocity(TerrainManager.GolfBall.rigid.velocity.y);

                float sqrMag = (GolfBallShotPreview.AimingPosition.position - TerrainManager.GolfBall.transform.position).sqrMagnitude;
                CameraManager.SetGolfBallSquareMagnitudeToAimingPosition(sqrMag);

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

                        if (positions.Length > 0)
                        {
                            GolfBallShotPreview.UpdateShotPreview(TerrainManager.GolfBall.Angle.ToString("0") + "°", TerrainManager.GolfBall.Angle, positions, TerrainManager.GolfBall.transform.rotation, out Vector3 shotPeakPos, out _);
                            CameraManager.SetShotPeakHeightFromGround((shotPeakPos - positions[0]).y);

                            MinimapManager.UpdateMinimapShotPreview(positions[positions.Length - 1], TerrainManager.GolfBall.Rotation);
                        }
                        else
                        {
                            Debug.LogError("Shot preview positions have not been calculated");
                        }




                        // Update the HUD to display the correct values
                        HUDManager.PowerSlider.DisplayValue.text = (TerrainManager.GolfBall.Power * 100).ToString("0") + "%";

                        // Set the power slider colour
                        Color p = HUDManager.PowerSlider.Gradient.Evaluate(TerrainManager.GolfBall.Power);
                        p.a = HUDManager.PowerSliderBackgroundAlpha;
                        HUDManager.PowerSlider.Background.color = p;

                        break;
                    // Flying mode
                    case GolfBall.PlayState.Flying:

                        break;
                    // Rolling mode
                    case GolfBall.PlayState.Rolling:
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

    private void UpdateHUDShotCounter()
    {
        // Update the shots counter
        HUDManager.Shots.text = TerrainManager.GolfBall.Progress.ShotsForThisHole.ToString();
    }

    private void UpdateHUDCourseProgressScreen()
    {
        GolfBall.Stats.Pot[] holes = TerrainManager.GolfBall.Progress.CoursesCompleted.ToArray();

        HUDManager.ScoreboardButton.SetActive(holes.Length > 0);

        if (holes != null && holes.Length > 0)
        {
            // Add rows until we have enough
            while (HUDManager.ScoreboardRows.Count < holes.Length)
            {
                GameObject g = Instantiate(HUDManager.ScoreRowPrefab, HUDManager.ScoreRowParent.transform);
                ScoreboardRow row = g.GetComponent<ScoreboardRow>();

                HUDManager.ScoreboardRows.Add(row);
            }

            // Update each ones data
            for (int holeIndex = 0; holeIndex < holes.Length; holeIndex++)
            {
                int rowIndex = HUDManager.ScoreboardRows.Count - 1 - holeIndex;

                HUDManager.ScoreboardRows[rowIndex].HoleNumber.text = $"#{holes[holeIndex].CourseNumber + 1}";
                HUDManager.ScoreboardRows[rowIndex].Shots.text = holes[holeIndex].ShotsTaken.ToString();


                // Set the time message 
                string timeMessage;
                if (holes[holeIndex].Time.Minutes > 0)
                {
                    timeMessage = $"{holes[holeIndex].Time.Minutes}m {holes[holeIndex].Time.TotalSeconds % 60:0.0}s";
                }
                else
                {
                    timeMessage = $"{holes[holeIndex].Time.TotalSeconds % 60:0.0}s";
                }

                HUDManager.ScoreboardRows[rowIndex].Time.text = timeMessage;
            }

        }
    }

    private void OnGameOver(GolfBall.Stats.Pot[] stats)
    {
        HUDManager.SetVisible(true);
        HUDManager.HideAllMenus();
        HUDManager.ShootingCanvas.SetActive(false);
        HUDManager.CanvasScoreboard.gameObject.SetActive(true);
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
        InGame = 3,
        CourseEnd = 4
    }
}
