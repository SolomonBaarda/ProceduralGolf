using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, IManager
{
    public static UnityEvent<TerrainGenerator.GenerationSettings> OnRequestStartGenerating = new UnityEvent<TerrainGenerator.GenerationSettings>();

    [Header("Managers")]
    public TerrainGenerator TerrainGenerator;
    public TerrainManager TerrainManager;

    private HUD HUD;

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
    private static readonly Gamerule Testing = new Gamerule(false, 0, false, false);
    private static readonly Gamerule FixedArea = new Gamerule(true, 1000, true, true);

    public delegate void CourseGenerated(TerrainData data);
    public delegate void PreviewGenerated(Texture2D map);

    [Space]
    public Material Skybox;

    private void Awake()
    {
        OnRequestStartGenerating.AddListener(StartGeneration);

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        RenderSettings.skybox = Skybox;
    }

    private void Start()
    {
        Clear();

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(false);
        }



    }

    public void SetVisible(bool visible)
    {
        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(visible);
        }

        TerrainManager.SetVisible(visible);
        TerrainGenerator.SetVisible(visible);
    }

    public void RestartGame()
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

    public void StartGeneration(TerrainGenerator.GenerationSettings settings)
    {
        if (TerrainGenerator.IsGenerating) return;

        LoadingScreen.Instance.SetVisible(true);

        foreach (GameObject g in ObjectsToDisableWhileLoading)
        {
            g.SetActive(false);
        }

        Clear();

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

        // Now generate the terrain
        TerrainGenerator.Generate(settings, LoadTerrain);
    }

    private void LoadTerrain(TerrainData data)
    {
        StartCoroutine(WaitUntilGameLoaded(data));
    }

    private IEnumerator WaitUntilGameLoaded(TerrainData data)
    {
        // Set up the TerrainManager
        TerrainManager.Set(Gamerules.HideFarChunks, Gamerules.ViewDistanceWorldUnits);
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
        RestartGame();

        if (Gamerules.UseHUD)
        {
            HUD.Compass.Following = TerrainManager.GolfBall.transform;
            HUD.SetVisible(true);
        }

        LoadingScreen.Instance.SetVisible(false);

        Logger.Log("Game has started. There are " + TerrainManager.CurrentLoadedTerrain.Courses.Count + " courses.");
    }





    private void Update()
    {
        if (Gamerules.UseGolfBall && TerrainManager.HasTerrain)
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

    private void ResetCameraTriggers()
    {
        foreach (string s in AllCameraTriggers)
        {
            CameraStates.ResetTrigger(s);
        }
    }

    public struct Gamerule
    {
        public bool HideFarChunks;
        public float ViewDistanceWorldUnits;

        public bool UseHUD;
        public bool UseGolfBall;

        public Gamerule(bool hideFarChunks, float viewDistanceWorldUnits, bool HUD, bool ball)
        {
            HideFarChunks = hideFarChunks;
            ViewDistanceWorldUnits = viewDistanceWorldUnits;
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
