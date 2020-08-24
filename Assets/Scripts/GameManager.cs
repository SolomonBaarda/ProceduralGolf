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

    [Space]
    public Material Skybox;

    private bool drawMap;


    [SerializeField] public List<TerrainData> WorldSaves = new List<TerrainData>();

    private void Awake()
    {
        HUD.OnHudLoaded += OnHUDLoaded;

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);

        HUDHasLoaded = false;

        CourseManager.AllTerrain = TerrainManager.TerrainChunkManager;
        CourseManager.GolfHoles = TerrainGenerator.GolfHoles;

        TerrainGenerator.TerrainChunkManager = TerrainManager.TerrainChunkManager;

        TerrainGenerator.OnInitialTerrainGenerated += InitialTerrainGenerated;
        TerrainGenerator.OnChunksUpdated += ChunksUpdated;
        TerrainGenerator.OnChunksUpdated += CourseManager.UpdateGolfHolesOrder;

        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        if(HUD != null)
        {
            HUD.OnShootPressed -= GolfBall.Shoot;
            HUD.OnShootPressed -= UpdateHUDShotCounter;

            HUD.OnRestartPressed -= ResetGame;
            HUD.OnQuitPressed -= Application.Quit;
        }



        TerrainGenerator.OnInitialTerrainGenerated -= InitialTerrainGenerated;
        TerrainGenerator.OnChunksUpdated -= ChunksUpdated;
        TerrainGenerator.OnChunksUpdated -= CourseManager.UpdateGolfHolesOrder;
    }


    private void Start()
    {
        StartCoroutine(WaitUntilGameStart());

        InvokeRepeating("CheckTerrainGeneration", 1, 2);
    }



    private IEnumerator WaitUntilGameStart()
    {
        LoadingScreen.Active(true);

        //TerrainData d = WorldSaves[0];
        //AssetLoader.LoadTerrain(ref d);


        // Load the map from file
        if (TerrainMode == TerrainGenerationMethod.LoadFromFile)
        {
            Gamerules = new Gamerule(false, true, 2, 400, true);

            // Get all of the world saves
            //List<TerrainData> maps = AssetLoader.GetAllWorldSaves();



            // Load the terrain data into the manager
            TerrainManager.LoadTerrain(WorldSaves[0]);
        }
        // Do endless terrain
        else if (TerrainMode == TerrainGenerationMethod.RealtimeEndless)
        {
            Gamerules = new Gamerule(true, true, 1, 400, true);

            TerrainGenerator.GenerateInitialTerrain(TerrainGenerator.GetAllPossibleNearbyChunks(TerrainManager.ORIGIN, Gamerules.InitialGenerationRadius));
        }
        // Generate a fixed area to save to file
        else if (TerrainMode == TerrainGenerationMethod.FixedArea)
        {
            Gamerules = new Gamerule(false, false, 2, 0, false);

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



    private void InitialTerrainGenerated()
    {
        TerrainManager.LoadTerrain(TerrainGenerator.TerrainData);
    }


    private void ChunksUpdated(IEnumerable<Vector2Int> chunks)
    {
        HashSet<TerrainChunkData> data = TerrainGenerator.GetChunkData(chunks);

        TerrainManager.AddChunks(data);
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

        HUD.OnRestartPressed += ResetGame;
        HUD.OnQuitPressed += Application.Quit;

        HUDHasLoaded = true;
    }


    private void ResetGame()
    {
        CourseManager.RespawnGolfBall(CourseManager.GetHole(0));
        UpdateHUDShotCounter();
    }


    private void UpdateHUDShotCounter()
    {
        HUD.Shots.text = GolfBall.CurrentHoleStats.ShotsForThisHole.ToString();
    }














    public struct Gamerule
    {
        public bool DoEndlessTerrain;
        public bool DoHideFarChunks;
        public int InitialGenerationRadius;
        public float ViewDistanceWorldUnits;

        public bool UseHUD;

        public Gamerule(bool endlessTerrain, bool hideFarChunks, int radius, float viewDistance, bool HUD)
        {
            DoEndlessTerrain = endlessTerrain;
            DoHideFarChunks = hideFarChunks;
            InitialGenerationRadius = radius;
            ViewDistanceWorldUnits = viewDistance;
            UseHUD = HUD;
        }
    }


    public enum TerrainGenerationMethod
    {
        LoadFromFile,
        RealtimeEndless,
        FixedArea,
    }
}
