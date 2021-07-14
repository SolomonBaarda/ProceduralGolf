﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator;
    public TerrainManager TerrainManager;

    public GolfBall GolfBall;
    public Follower BallFollower;

    public HUD HUD;
    private bool HUDHasLoaded;

    [Space]
    public TerrainGenerationMethod TerrainMode;
    private Gamerule Gamerules;
    private static readonly Gamerule FromFile = new Gamerule(false, true, 0, 550, true, true);
    private static readonly Gamerule RealtimeEndless = new Gamerule(true, true, 3, 400, true, true);
    private static readonly Gamerule Testing = new Gamerule(false, false, 1, 0, false, false);
    private static readonly Gamerule FixedArea = new Gamerule(false, true, 3, 2000, true, true);

    public delegate void CourseGenerated(TerrainData data);
    public delegate void PreviewGenerated(Texture2D map);


    [Space]
    public Material Skybox;


    [SerializeField] public List<TerrainData> WorldSaves = new List<TerrainData>();

    private void Awake()
    {
        HUD.OnHudLoaded += OnHUDLoaded;

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        HUDHasLoaded = false;

        TerrainGenerator.TerrainChunkManager = TerrainManager.TerrainChunkManager;



        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        if (HUD != null)
        {
            HUD.OnShootPressed -= GolfBall.Shoot;
            HUD.OnShootPressed -= UpdateHUDShotCounter;

            TerrainManager.OnHoleCompleted -= UpdateHUDShotCounter;

            HUD.OnRestartPressed -= ResetGame;
            HUD.OnQuitPressed -= Application.Quit;
        }
    }


    private void Start()
    {
        StartCoroutine(WaitUntilGameStart());
    }



    private IEnumerator WaitUntilGameStart()
    {
        LoadingScreen.Active(true);

        TerrainGenerator.OnGenerationStateChanged.RemoveAllListeners();
        TerrainGenerator.OnGenerationStateChanged.AddListener(info => { LoadingScreen.Instance.Info.text = info; });

        // Generate a fixed area for testing
        if (TerrainMode == TerrainGenerationMethod.Testing)
        {
            Gamerules = Testing;

            List<Vector2Int> l = TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius).ToList();
            yield return null;

            TerrainGenerator.Generate(l, LoadGame);
        }
        else if (TerrainMode == TerrainGenerationMethod.FixedArea)
        {
            Gamerules = FixedArea;

            List<Vector2Int> l = TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius).ToList();
            yield return null;

            TerrainGenerator.Generate(l, LoadGame);
        }
    }

    private void LoadGame(TerrainData data)
    {
        StartCoroutine(WaitForLoadGame(data));
    }

    private IEnumerator WaitForLoadGame(TerrainData data)
    {
        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }
        //Debug.Log("Finished generating");

        TerrainManager.LoadTerrain(data);

        // Ensure there is terrain before we start
        while (TerrainManager.IsLoading)
        {
            yield return null;
        }
        //Debug.Log("Finished loading terrain");


        if (!TerrainManager.HasTerrain)
        {
            Debug.LogError("Terrain manager does not have terrain");
            yield break;
        }


        // Set up the TerrainManager
        TerrainManager.Set(Gamerules.DoHideFarChunks, Gamerules.ViewDistanceWorldUnits);

        // Disable the golf ball if we dont need it
        GolfBall.gameObject.SetActive(Gamerules.UseGolfBall);


        // Load the HUD if we need it
        if (Gamerules.UseHUD)
        {
            SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);

            // Ensure the hud has loaded if we want it
            while (!HUDHasLoaded)
            {
                yield return null;
            }
            //Debug.Log("Finished loading HUD");

            HUD.Active(false);
        }

        if (Gamerules.UseGolfBall)
        {
            GolfBall.OnOutOfBounds += TerrainManager.UndoShot;
        }


        // Start the game
        ResetGame();

        if (Gamerules.UseHUD)
        {
            HUD.Compass.Following = GolfBall.transform;
            HUD.Active(true);
        }

        LoadingScreen.Active(false);

        Debug.Log("Game has started. There are " + TerrainManager.CurrentLoadedTerrain.Courses.Count + " courses.");
    }





    private void Update()
    {
        if (Gamerules.UseGolfBall)
        {
            // Taking a shot
            bool isShooting = GolfBall.State == GolfBall.PlayState.Shooting;
            // Set the preview active or not
            GolfBall.ShotPowerPreview.gameObject.SetActive(isShooting);
            GolfBall.ShotNormalPreview.gameObject.SetActive(isShooting);
            GolfBall.ShotAnglePreview.gameObject.SetActive(isShooting);


            if (Gamerules.UseHUD && HUD != null)
            {
                HUD.CanvasInteraction.gameObject.SetActive(true);
                HUD.MainHUD.SetActive(true);

                HUD.ShootingMenu.SetActive(isShooting);


                // Update the camera angles
                switch (GolfBall.State)
                {
                    // Shooting mode
                    case GolfBall.PlayState.Shooting:

                        int heldButtonMultiplier = 24;
                        int touchMultiplier = 10;

                        // Calculate the deltas for each 
                        Vector2 rotationAndAngleDelta = HUD.MainSlider.DeltaPosition * Time.deltaTime * touchMultiplier;
                        Vector2 powerDelta = HUD.PowerSlider.DeltaPosition * Time.deltaTime * touchMultiplier;

                        // Make sure power has priority over rotation and angle
                        if (powerDelta.x != 0 || powerDelta.y != 0)
                        {
                            rotationAndAngleDelta = Vector2.zero;
                        }

                        float rotationDelta = rotationAndAngleDelta.x;
                        float angleDelta = rotationAndAngleDelta.y;

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
                        GolfBall.SetValues(GolfBall.Rotation + rotationDelta, GolfBall.Angle + angleDelta, GolfBall.Power + powerDelta.y / 50f);


                        // Just use the one camera view for now

                        // Update the shot preview
                        GolfBall.SetShotPowerPreview(true, true, true);
                        GolfBall.SetShotNormalPreview();
                        GolfBall.SetShotAnglePreview(GolfBall.Angle.ToString("0") + "°");

                        // Update the camera 
                        BallFollower.CurrentView = Follower.View.ShootingBehind;

                        // Update the HUD to display the correct values
                        HUD.PowerSlider.DisplayValue.text = (GolfBall.Power * 100).ToString("0") + "%";

                        // Set the power slider colour
                        Color p = HUD.PowerSlider.Gradient.Evaluate(GolfBall.Power);
                        p.a = HUD.PowerSliderBackgroundAlpha;
                        HUD.PowerSlider.Background.color = p;
                        break;
                    // Flying mode
                    case GolfBall.PlayState.Flying:
                        BallFollower.CurrentView = Follower.View.Above;
                        break;
                    // Rolling mode
                    case GolfBall.PlayState.Rolling:
                        BallFollower.CurrentView = Follower.View.Behind;
                        break;
                }
            }
        }
    }


    public void GenerateAgain()
    {
        Clear();
        StartCoroutine(WaitUntilGameStart());
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

        HUD.OnShootPressed += GolfBall.Shoot;
        HUD.OnShootPressed += UpdateHUDShotCounter;

        TerrainManager.OnHoleCompleted += UpdateHUDShotCounter;

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



    private void UpdateHUDShotCounter()
    {
        if (HUD != null)
        {
            // Update the shots counter
            HUD.Shots.text = GolfBall.Progress.ShotsForThisHole.ToString();


            GolfBall.Stats.Pot[] holes = GolfBall.Progress.CoursesCompleted.ToArray();

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
        LoadFromFile,
        FixedArea,
        Testing
    }
}
