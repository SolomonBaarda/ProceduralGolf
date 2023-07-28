using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MainMenu : MonoBehaviour, IManager
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    public UnityEvent<TerrainGenerator.GenerationSettings> OnPressStartGame = new UnityEvent<TerrainGenerator.GenerationSettings>();
    public UnityEvent OnPressQuit = new UnityEvent();


    public Transform Camera;
    public float CameraSpinSpeed = 1.0f;

    public GameObject PlayMenuParent;

    public GameObject InvalidSeedText;

    public static MainMenu Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        InvalidSeedText.SetActive(false);

        GenerateRandomSeed();

        SeedInput.onValueChanged.AddListener(
            (e) =>
            {
                try
                {
                    Convert.ToInt32(SeedInput.text);
                }
                catch (System.OverflowException)
                {
                    // Clamp the input to the min/max i32 values
                    SeedInput.text = SeedInput.text[0] == '-' ? System.Int32.MinValue.ToString() : System.Int32.MaxValue.ToString();
                }
                catch (System.FormatException)
                {
                    SeedInput.text = "0";
                }
            }
        );
    }

    private void Update()
    {
        Camera.Rotate(Vector3.up, CameraSpinSpeed * Time.deltaTime, Space.World);
    }

    public void QuitGame()
    {
        OnPressQuit.Invoke();
    }

    public void StartGame()
    {
        try
        {
            Settings.Seed = Convert.ToInt32(SeedInput.text);
            Settings.GenerateLOD = true;
            OnPressStartGame.Invoke(Settings);
        }
        catch (System.Exception)
        {
        }
    }


    public void GenerateRandomSeed()
    {
        SeedInput.text = Noise.RandomSeed.ToString();

        // Invalid seed:
        //SeedInput.text = "-1793396896";
    }

    public void Clear()
    {

    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetLoading(bool loading)
    {
        PlayMenuParent.SetActive(!loading);
    }

    public void OpenBrokenVectorCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/12124");
    }
}
