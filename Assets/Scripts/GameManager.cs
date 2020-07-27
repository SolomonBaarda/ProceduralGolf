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





        TerrainGenerator.Generate();
    }

    private void OnDestroy()
    {
        HUD.OnHudLoaded -= SetHud;
        GolfBall.OnRollingFinished -= HUD.ResetShootingWindow;
    }







    private void Update()
    {
        if (Input.GetButtonDown("Submit") || (Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Began))
        {
            TerrainGenerator.Generate();
        }




        HUD.ShootingWindow.SetActive(GolfBall.State == GolfBall.PlayState.Shooting);


        // Update the camera angles
        switch (GolfBall.State)
        {
            // Shooting mode
            case GolfBall.PlayState.Shooting:
                // Update the HUD values
                HUD.UpdateShootingWindow(GolfBall.Rotation, GolfBall.Angle, GolfBall.Power);

                // Update the camera angle
                Follower.View cameraView = Follower.View.Behind;
                if (HUD.Rotation.isOn)
                {
                    cameraView = Follower.View.ShootingAbove;
                }
                else if (HUD.Angle.isOn)
                {
                    cameraView = Follower.View.ShootingLeft;
                }
                else if (HUD.Power.isOn)
                {
                    cameraView = Follower.View.ShootingBehind;
                }

                BallFollower.CurrentView = cameraView;
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
    }
}
