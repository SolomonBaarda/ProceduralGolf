using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator;
    public CourseManager CourseManager;

    public GolfBall GolfBall;
    public Follower BallFollower;

    public HUD HUD;
    private bool HUDLoaded;

    public bool DoEndlessTerrain = true;
    public float ViewDistanceWorld = 200;

    public Material Skybox;


    private void Awake()
    {
        HUD.OnHudLoaded += SetHud;
        SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);
        HUDLoaded = false;

        CourseManager.AllTerrain = TerrainGenerator.TerrainChunkManager;

        RenderSettings.skybox = Skybox;
    }

    private void OnDestroy()
    {
        HUD.OnHudLoaded -= SetHud;

        HUD.OnShootPressed -= GolfBall.Shoot;
        HUD.OnShootPressed -= UpdateHUDShotCounter;

        HUD.OnRestartPressed -= CourseManager.RespawnGolfBall;
        HUD.OnQuitPressed -= Application.Quit;
    }


    private void Start()
    {
        TerrainGenerator.GenerateInitialTerrain();

        InvokeRepeating("CheckTerrainGeneration", 1, 5);


        StartCoroutine(WaitForHUDLoadThenStart());
    }



    private IEnumerator WaitForHUDLoadThenStart()
    {
        while (!HUDLoaded)
        {
            yield return null;
        }

        // Start the game
        ResetGame();
    }



    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            TerrainGenerator.GenerateInitialTerrain();
        }

        // Taking a shot
        bool isShooting = GolfBall.State == GolfBall.PlayState.Shooting;
        HUD.ShootingWindow.SetActive(isShooting);
        GolfBall.SetShotPreviewVisible(isShooting);


        // Update the camera angles
        switch (GolfBall.State)
        {
            // Shooting mode
            case GolfBall.PlayState.Shooting:

                // Calculate the deltas for each 
                Vector2 rotationDelta = Controller.DeltaPosition(HUD.Rotation.TouchBounds) * Time.deltaTime * Controller.TouchMultiplier;
                Vector2 angleDelta = Controller.DeltaPosition(HUD.Angle.TouchBounds) * Time.deltaTime * Controller.TouchMultiplier;
                Vector2 powerDelta = Controller.DeltaPosition(HUD.Power.TouchBounds) * Time.deltaTime * Controller.TouchMultiplier;

                // Calculate the new values (take into consideration the slider height)
                float rotation = GolfBall.Rotation + rotationDelta.y;
                float angle = GolfBall.Angle + angleDelta.y;
                float power = GolfBall.Power + powerDelta.y / 50f;

                // Set the new values
                GolfBall.SetValues(rotation, angle, power);


                // Just use the one camera view for now

                // Update the shot preview
                GolfBall.SetShotPreview(true, true, true, 16);

                // Update the camera 
                BallFollower.CurrentView = Follower.View.ShootingBehind;


                // Update the HUD to display the correct values
                HUD.Rotation.DisplayValue.text = GolfBall.Rotation.ToString("0") + "°";
                HUD.Angle.DisplayValue.text = GolfBall.Angle.ToString("0") + "°";
                HUD.Power.DisplayValue.text = (GolfBall.Power * 100).ToString("0") + "%";

                // Set the 2 angle sliders to the same colour
                Color b = HUD.Rotation.Background.color;
                b.a = HUD.SliderBackgroundAlpha;
                HUD.Rotation.Background.color = b;
                HUD.Angle.Background.color = b;

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

    }



    private void CheckTerrainGeneration()
    {
        if (DoEndlessTerrain)
        {
            TerrainGenerator.CheckNearbyChunks(GolfBall.Position, ViewDistanceWorld);
        }
    }





    private void SetHud(HUD hud)
    {
        HUD = hud;

        HUD.OnShootPressed += GolfBall.Shoot;
        HUD.OnShootPressed += UpdateHUDShotCounter;

        HUD.OnRestartPressed += ResetGame;
        HUD.OnQuitPressed += Application.Quit;

        HUDLoaded = true;
    }


    private void ResetGame()
    {
        CourseManager.RespawnGolfBall();
        UpdateHUDShotCounter();
    }


    private void UpdateHUDShotCounter()
    {
        HUD.Shots.text = GolfBall.GameStats.Shots.ToString();
    }
}
