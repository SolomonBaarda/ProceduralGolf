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
    public Button Shoot;


    [Header("Sliders")]
    public TouchScreenSlider Rotation;
    public TouchScreenSlider Angle;
    public TouchScreenSlider Power;
    [Range(0,1)]
    public float SliderBackgroundAlpha = 0.75f;


    private void Start()
    {
        OnHudLoaded.Invoke(this);

        Shoot.onClick.AddListener(ShootPressed);
    }


    private void OnDestroy()
    {
        Shoot.onClick.RemoveAllListeners();
    }


    private void ShootPressed() { OnShootPressed.Invoke(); }




}
