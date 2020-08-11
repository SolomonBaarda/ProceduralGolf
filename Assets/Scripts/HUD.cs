﻿using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public const string SceneName = "HUD";
    public static UnityAction<HUD> OnHudLoaded;

    public UnityAction OnShootPressed;
    public UnityAction OnRestartPressed;
    public UnityAction OnQuitPressed;

    public Canvas Canvas;

    [Header("Shooting Window")]
    public GameObject ShootingWindow;
    public Button Shoot;

    [Header("Sliders")]
    public TouchScreenSlider Rotation;
    public TouchScreenSlider Angle;
    public TouchScreenSlider Power;
    [Range(0,1)]
    public float SliderBackgroundAlpha = 0.75f;


    [Header("Main UI")]
    public Button Restart;
    public Button Quit;
    public GameObject ShotsDisplayParent;
    public TMP_Text Shots;


    [Header("Map")]
    public Transform MapParent;
    public RectTransform MapVisual;



    private void Start()
    {
        OnHudLoaded.Invoke(this);

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
