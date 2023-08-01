using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class HUDManager : MonoBehaviour, IManager
{
    public UnityEvent OnShootPressed;
    public UnityEvent OnRestartPressed;
    public UnityEvent OnQuitToMenuPressed;

    public Canvas CanvasInteraction;
    public Canvas CanvasScoreboard;
    public Canvas CanvasOptions;
    [Space]
    public GameObject ShootingMenu;
    public GameObject MainHUD;

    [Header("Shooting Window")]
    public PointerSlider MainSlider;
    [Range(0, 1)] public float PowerSliderBackgroundAlpha = 0.75f;
    public PointerSlider PowerSlider;
    [Space]
    public HeldButton RotationLess;
    public HeldButton RotationMore;
    [Space]
    public HeldButton AngleLess;
    public HeldButton AngleMore;


    [Header("Main UI")]
    public GameObject ShotsDisplayParent;
    public TMP_Text Shots;

    public Compass Compass;

    [Header("Scoreboard")]
    public GameObject ScoreRowPrefab;
    public GameObject ScoreRowParent;
    public List<ScoreboardRow> ScoreboardRows = new List<ScoreboardRow>();

    [Header("Minimap")]
    public GameObject Minimap;

    public void Reset()
    {
        for (int i = 0; i < ScoreRowParent.transform.childCount; i++)
        {
            Destroy(ScoreRowParent.transform.GetChild(i).gameObject);
        }
        ScoreboardRows.Clear();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        HideAllMenus();
    }

    private void HideAllMenus()
    {
        CanvasScoreboard.gameObject.SetActive(false);
        CanvasOptions.gameObject.SetActive(false);
    }

    public void ShootPressed() { OnShootPressed.Invoke(); HideAllMenus(); }
    public void RestartPressed() { OnRestartPressed.Invoke(); Reset(); HideAllMenus(); }
    public void QuitToMenuPressed() { OnQuitToMenuPressed.Invoke(); HideAllMenus(); }



}
