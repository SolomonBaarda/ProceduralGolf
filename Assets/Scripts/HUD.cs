using System.Collections.Generic;
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

    public Canvas CanvasInteraction;
    public Canvas CanvasScoreboard;
    [Space]
    public GameObject ShootingMenu;
    public GameObject MainHUD;

    [Header("Shooting Window")]
    public PointerSlider MainSlider;
    [Range(0, 1)] public float PowerSliderBackgroundAlpha = 0.75f;
    public PointerSlider PowerSlider;
    public Button Shoot;
    [Space]
    public HeldButton RotationLess;
    public HeldButton RotationMore;
    [Space]
    public HeldButton AngleLess;
    public HeldButton AngleMore;


    [Header("Main UI")]
    public Button ShowScoreboardButton;
    public bool ShowScoreboard;
    public Button Restart;
    public Button Quit;
    public GameObject ShotsDisplayParent;
    public TMP_Text Shots;

    [Header("Scoreboard")]
    public Button HideScoreboardButton;
    public GameObject ScoreRowPrefab;
    public GameObject ScoreRowParent;
    public List<ScoreboardRow> ScoreboardRows = new List<ScoreboardRow>();

    public static HUD Instance;


    private void Awake()
    {
        Instance = FindObjectOfType<HUD>();

        ShowScoreboardButton.onClick.AddListener(InvertScoreboard);
        HideScoreboardButton.onClick.AddListener(InvertScoreboard);
    }



    public void Active(bool visible)
    {
        CanvasInteraction.gameObject.SetActive(visible);
        CanvasScoreboard.gameObject.SetActive(visible);
    }


    private void InvertScoreboard()
    {
        ShowScoreboard = !ShowScoreboard;
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
        ShowScoreboardButton.onClick.RemoveAllListeners();
        HideScoreboardButton.onClick.RemoveAllListeners();
    }


    private void ShootPressed() { OnShootPressed.Invoke(); }
    private void RestartPressed() { OnRestartPressed.Invoke(); }
    private void QuitPressed() { OnQuitPressed.Invoke(); }


    public void Clear()
    {
        for(int i = 0; i < ScoreRowParent.transform.childCount; i++)
        {
            Destroy(ScoreRowParent.transform.GetChild(i).gameObject);
        }
        ScoreboardRows.Clear();
    }

}
