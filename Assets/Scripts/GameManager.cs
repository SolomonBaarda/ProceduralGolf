using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator;

    public GolfBall GolfBall;
    public Follower BallFollower;

    public HUD HUD;




    private void Awake()
    {
        HUD.OnHudLoaded += SetHud;
        SceneManager.LoadSceneAsync(HUD.SceneName, LoadSceneMode.Additive);
    }

    private void OnDestroy()
    {
        HUD.OnHudLoaded -= SetHud;

        GolfBall.OnRollingFinished -= HUD.ResetShootingWindow;
        HUD.OnShootPressed -= GolfBall.Shoot;
    }


    private void Start()
    {
        TerrainGenerator.Generate();
    }




    private void Update()
    {
        /*
        if (Input.GetButtonDown("Submit") || (Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Began))
        {
            TerrainGenerator.Generate();
        }
        */


        // Taking a shot
        bool isShooting = GolfBall.State == GolfBall.PlayState.Shooting;
        HUD.ShootingWindow.SetActive(isShooting);
        GolfBall.SetShotPreviewVisible(isShooting);


        // Update the camera angles
        switch (GolfBall.State)
        {
            // Shooting mode
            case GolfBall.PlayState.Shooting:
                // Update the HUD values
                HUD.UpdateShootingWindow(GolfBall.Rotation, GolfBall.Angle, GolfBall.Power);

                // Update the camera angle
                Follower.View cameraView = Follower.View.Behind;
                bool useRotation = false, useAngle = false, usePower = false;
                float shotPreviewLengthMultiplier = 1;



                Vector2 deltaPosition = Controller.GetDeltaPositionScaled() * Controller.TouchMultiplier;
                float rotation = GolfBall.Rotation, angle = GolfBall.Angle, power = GolfBall.Power;

                // Setting rotation now
                if (HUD.Rotation.isOn)
                {
                    cameraView = Follower.View.ShootingAbove;
                    useRotation = true;
                    shotPreviewLengthMultiplier = 3;
                    rotation += deltaPosition.x;
                }
                // Angle
                else if (HUD.Angle.isOn)
                {
                    cameraView = Follower.View.ShootingLeft;
                    useAngle = true;
                    useRotation = true;
                    shotPreviewLengthMultiplier = 1.5f;
                    angle += deltaPosition.y;
                }
                // Power
                else if (HUD.Power.isOn)
                {
                    cameraView = Follower.View.ShootingBehind;
                    useRotation = true;
                    useAngle = true;
                    usePower = true;
                    shotPreviewLengthMultiplier = 16;
                    power += deltaPosition.y;
                }

                // Update the camera 
                BallFollower.CurrentView = cameraView;

                // Update the golf ball
                GolfBall.SetValues(rotation, angle, power);
                GolfBall.SetShotPreview(useRotation, useAngle, usePower, shotPreviewLengthMultiplier);

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






    private void SetHud(HUD hud)
    {
        HUD = hud;

        GolfBall.OnRollingFinished += HUD.ResetShootingWindow;

        HUD.OnShootPressed += GolfBall.Shoot;
    }
}
