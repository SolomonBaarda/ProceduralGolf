﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator;
    public CourseManager CourseManager;
    public Minimap Minimap;

    public GolfBall GolfBall;
    public Follower BallFollower;

    public HUD HUD;
    private bool HUDHasLoaded;

    [Space]
    public bool DoEndlessTerrain = true;
    public float ViewDistanceWorld = 200;

    [Space]
    public Material Skybox;

    private bool drawMap;



    private void Awake()
    {
        HUD.OnHudLoaded += OnHUDLoaded;

        SceneManager.LoadScene(LoadingScreen.SceneName, LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);

        HUDHasLoaded = false;

        CourseManager.AllTerrain = TerrainGenerator.TerrainChunkManager;
        CourseManager.GolfHoles = TerrainGenerator.GolfHoles;

        TerrainGenerator.OnChunksUpdated += CourseManager.UpdateGolfHolesOrder;
        TerrainGenerator.OnChunkGenerated += NewChunkAdded;

        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        HUD.OnShootPressed -= GolfBall.Shoot;
        HUD.OnShootPressed -= UpdateHUDShotCounter;

        HUD.OnRestartPressed -= ResetGame;
        HUD.OnQuitPressed -= Application.Quit;


        TerrainGenerator.OnChunksUpdated -= CourseManager.UpdateGolfHolesOrder;
        TerrainGenerator.OnChunkGenerated -= NewChunkAdded;

    }


    private void Start()
    {
        StartCoroutine(WaitUntilGameStart());

        InvokeRepeating("CheckTerrainGeneration", 1, 2);
    }



    private IEnumerator WaitUntilGameStart()
    {
        LoadingScreen.Active(true);

        TerrainGenerator.GenerateInitialTerrain(TerrainGenerator.TerrainChunkManager.ChunkSizeWorldUnits * 2);

        // Ensure the hud has loaded
        while (!HUDHasLoaded)
        {
            yield return null;
        }

        HUD.Active(false);


        // Ensure the initial terrain has been fully generated 
        while (!TerrainGenerator.InitialTerrainGenerated)
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


        HUD.Active(true);
        LoadingScreen.Active(false);

        Debug.Log("Game has started.");
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TerrainGenerator.Clear();
            CourseManager.Clear();
            StartCoroutine(WaitUntilGameStart());
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
                if(HUD.RotationLess.IsPressed && !HUD.RotationMore.IsPressed)
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








    private void CheckTerrainGeneration()
    {
        if (DoEndlessTerrain)
        {
            TerrainGenerator.TryGenerateNearbyChunks(GolfBall.Position, ViewDistanceWorld);
        }
    }



    private void NewChunkAdded(Vector2Int chunk)
    {
        TerrainChunk c = TerrainGenerator.TerrainChunkManager.GetChunk(chunk);

        Texture2D map = TextureGenerator.GenerateTexture(TextureGenerator.GenerateTextureData(c.TerrainMap, TerrainGenerator.Texture_MapSettings));
        Minimap.AddChunk(chunk, map);
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
}
