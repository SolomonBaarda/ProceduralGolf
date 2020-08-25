using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator;
    public TerrainManager TerrainManager;
    public CourseManager CourseManager;
    public Minimap Minimap;

    public GolfBall GolfBall;
    public Follower BallFollower;

    public HUD HUD;
    private bool HUDHasLoaded;

    [Space]
    public TerrainGenerationMethod TerrainMode;
    private Gamerule Gamerules;
    private static readonly Gamerule FromFile = new Gamerule(false, true, 0, 550, true, true);
    private static readonly Gamerule RealtimeEndless = new Gamerule(true, true, 3, 400, true, true);
    private static readonly Gamerule FixedArea = new Gamerule(false, false, 3, 0, false, false);



    [Space]
    public Material Skybox;

    private bool drawMap;


    [SerializeField] public List<TerrainData> WorldSaves = new List<TerrainData>();

    private void Awake()
    {
        HUD.OnHudLoaded += OnHUDLoaded;

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        HUDHasLoaded = false;

        CourseManager.TerrainManager = TerrainManager;
        TerrainGenerator.TerrainChunkManager = TerrainManager.TerrainChunkManager;

        TerrainGenerator.OnInitialTerrainGenerated += InitialTerrainGenerated;
        TerrainGenerator.OnChunksUpdated += ChunksUpdated;

        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        if (HUD != null)
        {
            HUD.OnShootPressed -= GolfBall.Shoot;
            HUD.OnShootPressed -= UpdateHUDShotCounter;

            CourseManager.OnHoleCompleted -= UpdateHUDShotCounter;

            HUD.OnRestartPressed -= ResetGame;
            HUD.OnQuitPressed -= Application.Quit;
        }



        TerrainGenerator.OnInitialTerrainGenerated -= InitialTerrainGenerated;
        TerrainGenerator.OnChunksUpdated -= ChunksUpdated;
    }


    private void Start()
    {
        StartCoroutine(WaitUntilGameStart());

        InvokeRepeating("CheckTerrainGeneration", 1, 2);
    }



    private IEnumerator WaitUntilGameStart()
    {
        LoadingScreen.Active(true);

        // Load the map from file
        if (TerrainMode == TerrainGenerationMethod.LoadFromFile)
        {
            DateTime before = DateTime.Now;

            Gamerules = FromFile;

            TerrainData d = Instantiate(WorldSaves[0]);

            // Load the terrain data into the manager
            TerrainManager.LoadTerrain(d, before);


            // Force the coursemanager to order the holes
            CourseManager.UpdateGolfHoles(d.GolfHoles);
        }
        // Do endless terrain
        else if (TerrainMode == TerrainGenerationMethod.RealtimeEndless)
        {
            Gamerules = RealtimeEndless;

            TerrainGenerator.GenerateInitialTerrain(TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius));
        }
        // Generate a fixed area to save to file
        else if (TerrainMode == TerrainGenerationMethod.FixedArea)
        {
            Gamerules = FixedArea;

            TerrainGenerator.GenerateInitialTerrain(TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius));
        }


        TerrainManager.Set(Gamerules.DoHideFarChunks, GolfBall.transform, Gamerules.ViewDistanceWorldUnits);


        // Load the HUD if we need it
        if (Gamerules.UseHUD)
        {
            SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);


            // Ensure the hud has loaded if we want it
            while (!HUDHasLoaded)
            {
                yield return null;
            }

            HUD.Active(false);
        }

        if (!Gamerules.UseGolfBall)
        {
            GolfBall.gameObject.SetActive(false);
        }

        // Ensure there is terrain before we start
        while (!TerrainManager.HasTerrain)
        {
            yield return null;
        }


        // Ensure all of the holes have been correctly numbered
        while (!CourseManager.HolesHaveBeenOrdered)
        {
            yield return null;
        }

        // Start the game
        ResetGame();

        if (Gamerules.UseHUD)
        {
            HUD.Active(true);
        }

        LoadingScreen.Active(false);

        Debug.Log("Game has started.");
    }


    private void Update()
    {
        if (TerrainMode == TerrainGenerationMethod.FixedArea)
        {
            // Generate again
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TerrainGenerator.Clear();
                TerrainManager.Clear();
                CourseManager.Clear();
                StartCoroutine(WaitUntilGameStart());
            }


            // Save map to file
            if (Input.GetKeyDown(KeyCode.P))
            {
                // Save the currently used TerrainData to file
                AssetLoader.SaveTerrain(TerrainManager.CurrentLoadedTerrain);
            }
        }


        drawMap = Input.GetKey(KeyCode.Tab);



        if (Gamerules.UseGolfBall)
        {

            // Taking a shot
            bool isShooting = GolfBall.State == GolfBall.PlayState.Shooting;
            // Set the preview active or not
            GolfBall.ShotPowerPreview.gameObject.SetActive(isShooting);
            GolfBall.ShotNormalPreview.gameObject.SetActive(isShooting);
            GolfBall.ShotAnglePreview.gameObject.SetActive(isShooting);


            // Update the camera angles
            switch (GolfBall.State)
            {
                // Shooting mode
                case GolfBall.PlayState.Shooting:

                    int buttonMultiplier = 24;

                    // Calculate the deltas for each 
                    Vector2 rotationAndAngleDelta = Controller.DeltaPosition(HUD.ShootingWindow) * Time.deltaTime * Controller.TouchMultiplier;
                    Vector2 powerDelta = Controller.DeltaPosition(HUD.Power.TouchBounds) * Time.deltaTime * Controller.TouchMultiplier;

                    float rotationDelta = rotationAndAngleDelta.x;
                    float angleDelta = rotationAndAngleDelta.y;

                    // Rotation
                    // Move less
                    if (HUD.RotationLess.IsPressed && !HUD.RotationMore.IsPressed)
                    {
                        rotationDelta = -buttonMultiplier * Time.deltaTime;
                    }
                    // Move more
                    else if (!HUD.RotationLess.IsPressed && HUD.RotationMore.IsPressed)
                    {
                        rotationDelta = buttonMultiplier * Time.deltaTime;
                    }

                    // Angle
                    // Move less
                    if (HUD.AngleLess.IsPressed && !HUD.AngleMore.IsPressed)
                    {
                        angleDelta = -buttonMultiplier * Time.deltaTime;
                    }
                    // Move more
                    else if (!HUD.AngleLess.IsPressed && HUD.AngleMore.IsPressed)
                    {
                        angleDelta = buttonMultiplier * Time.deltaTime;
                    }

                    // Calculate the new values
                    float rotation = GolfBall.Rotation + rotationDelta;
                    float angle = GolfBall.Angle + angleDelta;
                    float power = GolfBall.Power + powerDelta.y / 50f;

                    // Set the new values
                    GolfBall.SetValues(rotation, angle, power);


                    // Just use the one camera view for now

                    // Update the shot preview
                    GolfBall.SetShotPowerPreview(true, true, true);
                    GolfBall.SetShotNormalPreview();
                    GolfBall.SetShotAnglePreview(GolfBall.Angle.ToString("0") + "°");

                    // Update the camera 
                    BallFollower.CurrentView = Follower.View.ShootingBehind;


                    // Update the HUD to display the correct values
                    //HUD.Rotation.DisplayValue.text = GolfBall.Rotation.ToString("0") + "°";
                    //HUD.Angle.DisplayValue.text = GolfBall.Angle.ToString("0") + "°";
                    HUD.Power.DisplayValue.text = (GolfBall.Power * 100).ToString("0") + "%";

                    // Set the power slider colour
                    Color p = HUD.Power.Gradient.Evaluate(GolfBall.Power);
                    p.a = HUD.SliderBackgroundAlpha;
                    HUD.Power.Background.color = p;

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


            // Make minimap visible
            Minimap.SetVisible(drawMap);
            if (HUDHasLoaded)
            {
                HUD.MapParent.gameObject.SetActive(drawMap);
                // Disable the shots counter in the minimap
                HUD.ShotsDisplayParent.SetActive(!drawMap);

                // Show the shooting window
                HUD.ShootingWindow.gameObject.SetActive(!drawMap && isShooting);
            }
        }
    }



    private void InitialTerrainGenerated()
    {
        TerrainManager.LoadTerrain(TerrainGenerator.TerrainData, DateTime.MinValue);
    }


    private void ChunksUpdated(IEnumerable<Vector2Int> chunks)
    {

        // Update the chunk visuals 
        TerrainManager.AddChunks(TerrainGenerator.GetChunkData(chunks));


        // Update all of the golf holes
        CourseManager.UpdateGolfHoles(TerrainGenerator.GetHoleData());
    }


    private void CheckTerrainGeneration()
    {
        if (Gamerules.DoEndlessTerrain)
        {
            // Try and generate all the chunks that we need to
            TerrainGenerator.TryGenerateChunks(TerrainGenerator.GetNearbyChunksToGenerate(GolfBall.Position, Gamerules.ViewDistanceWorldUnits), TerrainGenerator.Seed);
        }
    }




    private void OnHUDLoaded()
    {
        HUD = HUD.Instance;

        HUD.OnShootPressed += GolfBall.Shoot;
        HUD.OnShootPressed += UpdateHUDShotCounter;

        CourseManager.OnHoleCompleted += UpdateHUDShotCounter;

        HUD.OnRestartPressed += ResetGame;
        HUD.OnQuitPressed += Application.Quit;

        HUDHasLoaded = true;
    }


    private void ResetGame()
    {
        if (Gamerules.UseGolfBall)
        {
            CourseManager.Restart();
        }
        if(Gamerules.UseHUD)
        {
            UpdateHUDShotCounter();
        }
    }



    private void UpdateHUDShotCounter()
    {
        HUD.Shots.text = GolfBall.Progress.ShotsForThisHole.ToString();
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
        RealtimeEndless,
        FixedArea,
    }
}
