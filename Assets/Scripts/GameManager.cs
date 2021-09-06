using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    public TerrainGenerator TerrainGenerator;
    public TerrainManager TerrainManager;
    [HideInInspector]
    public HUD HUD;
    private bool HUDHasLoaded;

    [Header("Golf Ball")]
    public ShotPreview GolfBallShotPreview;

    [Space]
    public List<GameObject> ObjectsToDisableWhileLoading;

    [Header("Camera")]
    public Animator CameraStates;
    public const string CameraSqrMagToTargetFloat = "SqrMagToTarget";
    public const string CameraAimingTrigger = "IsAiming", CameraRollingTrigger = "IsRolling", CameraFlyingTrigger = "IsFlying";
    private readonly string[] AllCameraTriggers = { CameraAimingTrigger, CameraRollingTrigger, CameraFlyingTrigger };

    [Header("Terrain settings")]
    public TerrainGenerationMethod TerrainMode;
    private Gamerule Gamerules;
    private static readonly Gamerule Testing = new Gamerule(false, false, 3, 0, false, false);
    private static readonly Gamerule FixedArea = new Gamerule(false, true, 3, 2000, true, true);

    public delegate void CourseGenerated(TerrainData data);
    public delegate void PreviewGenerated(Texture2D map);

    [Space]
    public Material Skybox;

    private void Awake()
    {
        HUD.OnHudLoaded += OnHUDLoaded;

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        HUDHasLoaded = false;


        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        if (HUD != null)
        {
            HUD.OnShootPressed -= TerrainManager.GolfBall.Shoot;
            HUD.OnShootPressed -= UpdateHUDShotCounter;

            TerrainManager.OnCourseCompleted -= UpdateHUDShotCounter;

            HUD.OnRestartPressed -= ResetGame;
            HUD.OnQuitPressed -= Application.Quit;
        }
    }

    private void Start()
    {
        LoadGame();
    }

    private void LoadGame()
    {
        LoadingScreen.Active(true);

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(false);
        }

        Logger.OnLogMessage.AddListener(message => { LoadingScreen.Instance.Info.text = message; });

        // Set the game rules
        switch (TerrainMode)
        {
            case TerrainGenerationMethod.FixedArea:
                Gamerules = FixedArea;
                break;
            case TerrainGenerationMethod.Testing:
                Gamerules = Testing;
                break;
        }

        // Now generate the terraub
        List<Vector2Int> l = TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius).ToList();
        TerrainGenerator.Generate(l, LoadTerrain);
    }

    private void LoadTerrain(TerrainData data)
    {
        StartCoroutine(WaitUntilGameLoaded(data));
    }

    private IEnumerator WaitUntilGameLoaded(TerrainData data)
    {
        // Set up the TerrainManager
        TerrainManager.Set(Gamerules.DoHideFarChunks, Gamerules.ViewDistanceWorldUnits);
        TerrainManager.LoadTerrain(data);

        // Ensure there is terrain before we start
        while (TerrainManager.IsLoading) yield return null;

        // Sanity check
        if (!TerrainManager.HasTerrain)
        {
            Debug.LogError("Terrain manager does not have terrain");
            yield break;
        }

        // Load the HUD if we need it
        if (Gamerules.UseHUD)
        {
            SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);
            while (!HUDHasLoaded) yield return null;
            HUD.Active(false);
        }

        if (Gamerules.UseGolfBall)
        {
            TerrainManager.GolfBall.OnOutOfBounds += TerrainManager.UndoShot;
        }

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(true);
        }

        // Disable the golf ball if we dont need it
        TerrainManager.GolfBall.gameObject.SetActive(Gamerules.UseGolfBall);

        // Start the game
        ResetGame();

        if (Gamerules.UseHUD)
        {
            HUD.Compass.Following = TerrainManager.GolfBall.transform;
            HUD.Active(true);
        }

        LoadingScreen.Active(false);

        Logger.Log("Game has started. There are " + TerrainManager.CurrentLoadedTerrain.Courses.Count + " courses.");
    }





    private void Update()
    {
        if (Gamerules.UseGolfBall)
        {
            // Taking a shot
            bool isAiming = TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming;

            // Set the preview active or not
            GolfBallShotPreview.gameObject.SetActive(isAiming);

            if (Gamerules.UseHUD && HUD != null)
            {
                HUD.ShootingMenu.SetActive(isAiming);
                HUD.Minimap.SetActive(isAiming);

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
                        TerrainManager.GolfBall.SetValues(TerrainManager.GolfBall.Rotation + rotationDelta, TerrainManager.GolfBall.Angle + angleDelta, TerrainManager.GolfBall.Power + powerDelta.y / 50f);


                        // Update the shot preview
                        Vector3[] positions = TerrainManager.GolfBall.CalculateShotPreviewWorldPositions(1000, 0.1f).ToArray();
                        GolfBallShotPreview.SetShotPreviewPoints(TerrainManager.GolfBall.Angle.ToString("0") + "°", TerrainManager.GolfBall.Angle, positions, TerrainManager.GolfBall.transform.rotation);

                        // Update the HUD to display the correct values
                        HUD.PowerSlider.DisplayValue.text = (TerrainManager.GolfBall.Power * 100).ToString("0") + "%";

                        // Set the power slider colour
                        Color p = HUD.PowerSlider.Gradient.Evaluate(TerrainManager.GolfBall.Power);
                        p.a = HUD.PowerSliderBackgroundAlpha;
                        HUD.PowerSlider.Background.color = p;

                        ResetCameraTriggers();
                        CameraStates.SetTrigger(CameraAimingTrigger);
                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, -1);
                        break;
                    // Flying mode
                    case GolfBall.PlayState.Flying:
                        ResetCameraTriggers();
                        CameraStates.SetTrigger(CameraFlyingTrigger);
                        float sqrMag = (GolfBallShotPreview.ShotPreviewTarget.position - TerrainManager.GolfBall.transform.position).sqrMagnitude;
                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, sqrMag);
                        break;
                    // Rolling mode
                    case GolfBall.PlayState.Rolling:
                        ResetCameraTriggers();
                        CameraStates.SetTrigger(CameraRollingTrigger);
                        CameraStates.SetFloat(CameraSqrMagToTargetFloat, -1);
                        break;
                }
            }
        }
    }


    public void GenerateAgain()
    {
        Clear();
        LoadGame();
    }

    public void Clear()
    {
        TerrainManager.Clear();

        if (HUD)
            HUD.Clear();
    }


    private void OnHUDLoaded()
    {
        HUD = HUD.Instance;

        HUD.OnShootPressed += TerrainManager.GolfBall.Shoot;
        HUD.OnShootPressed += UpdateHUDShotCounter;

        TerrainManager.OnCourseCompleted += UpdateHUDShotCounter;

        HUD.OnRestartPressed += ResetGame;
        HUD.OnQuitPressed += Application.Quit;

        HUDHasLoaded = true;
    }


    private void ResetGame()
    {
        if (Gamerules.UseGolfBall)
        {
            TerrainManager.Restart();
        }
        if (Gamerules.UseHUD)
        {
            UpdateHUDShotCounter();
        }
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


    private void ResetCameraTriggers()
    {
        foreach (string s in AllCameraTriggers)
        {
            CameraStates.ResetTrigger(s);
        }
    }




    public struct Gamerule
    {
        public bool DoEndlessTerrain;
        public bool DoHideFarChunks;
        public int InitialGenerationRadius;
        public float ViewDistanceWorldUnits;

        public bool UseHUD;
        public bool UseGolfBall;

        public Gamerule(bool endlessTerrain, bool hideFarChunks, int radius, float viewDistance, bool HUD, bool ball)
        {
            DoEndlessTerrain = endlessTerrain;
            DoHideFarChunks = hideFarChunks;
            InitialGenerationRadius = radius;
            ViewDistanceWorldUnits = viewDistance;
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
