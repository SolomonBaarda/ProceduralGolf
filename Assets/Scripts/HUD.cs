using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public const string SceneName = "HUD";
    public static UnityAction<HUD> OnHudLoaded;

    public GameObject ShootingWindow;

    public Toggle Rotation, Angle, Power;
    public TMP_Text ShotValue;


    private void Start()
    {
        ResetShootingWindow();
        OnHudLoaded.Invoke(this);
    }


    public void UpdateShootingWindow(float rotation, float angle, float power)
    {
        // Calculate what the text should say
        string text = "";
        if (Rotation.isOn)
        {
            text = rotation.ToString("0.0") + " degrees";
        }
        else if (Angle.isOn)
        {
            text = angle.ToString("0.0") + " degrees";
        }
        else if (Power.isOn)
        {
            text = power.ToString("0.0") + " power";
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
        Rotation.isOn = false;
        Rotation.isOn = true;
    }

}
