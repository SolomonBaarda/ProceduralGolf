using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public const string SceneName = "HUD";
    public static UnityAction<HUD> OnHudLoaded;

    public UnityAction OnShootPressed;


    public GameObject ShootingWindow;

    public Toggle Rotation, Angle, Power;
    public TMP_Text ShotValue;
    public Button Shoot;


    private void Start()
    {
        ResetShootingWindow();
        OnHudLoaded.Invoke(this);

        Shoot.onClick.AddListener(ShootPressed);
    }


    private void OnDestroy()
    {
        Shoot.onClick.RemoveAllListeners();
    }


    private void ShootPressed()
    {
        OnShootPressed.Invoke();
    }



    public void UpdateShootingWindow(float rotation, float angle, float power)
    {
        // Calculate what the text should say
        string text = "";
        if (Rotation.isOn)
        {
            text = rotation.ToString("0") + "°";
        }
        else if (Angle.isOn)
        {
            text = angle.ToString("0") + "°";
        }
        else if (Power.isOn)
        {
            text = (power * 100).ToString("0") + "%";
        }
        // Nothing is on
        else
        {
            // Set the first toggle to be on and try again
            ResetShootingWindow();
            UpdateShootingWindow(rotation, angle, power);
        }

        ShotValue.text = text;
    }

    public void ResetShootingWindow()
    {
        // Enable another, then rotation last to force it to update
        Rotation.isOn = false;
        Rotation.isOn = true;
    }

}
