using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public const string SceneName = "HUD";
    public static UnityAction OnHudLoaded;

    public UnityAction OnShootPressed;
    public UnityAction OnRestartPressed;
    public UnityAction OnQuitPressed;

    public Canvas CanvasShootingMenu;
    public Canvas CanvasMainUI;

    [Header("Shooting Window")]
    public PointerSlider ShootingSliderArea;
    public Button Shoot;

    [Header("Sliders")]
    public HeldButton RotationLess;
    public HeldButton RotationMore;
    [Space]
    public HeldButton AngleLess;
    public HeldButton AngleMore;
    [Space]
    public PointerSlider Power;
    [Range(0, 1)]
    public float SliderBackgroundAlpha = 0.75f;


    [Header("Main UI")]
    public Button Restart;
    public Button Quit;
    public GameObject ShotsDisplayParent;
    public TMP_Text Shots;



    public static HUD Instance;


    private void Awake()
    {
        Instance = FindObjectOfType<HUD>();
    }



    public void Active(bool visible)
    {
        CanvasShootingMenu.gameObject.SetActive(visible);
        CanvasMainUI.gameObject.SetActive(visible);
    }



    private void Start()
    {
        OnHudLoaded.Invoke();

        Shoot.onClick.AddListener(ShootPressed);
        Restart.onClick.AddListener(RestartPressed);
        Quit.onClick.AddListener(QuitPressed);
    }


    private void OnDestroy()
    {
        Shoot.onClick.RemoveAllListeners();
        Restart.onClick.RemoveAllListeners();
        Quit.onClick.RemoveAllListeners();
    }


    private void ShootPressed() { OnShootPressed.Invoke(); }
    private void RestartPressed() { OnRestartPressed.Invoke(); }
    private void QuitPressed() { OnQuitPressed.Invoke(); }




}
