using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class MainMenuManager : MonoBehaviour, IManager
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    public UnityEvent<TerrainGenerator.GenerationSettings, TerrainSettings, VolumeProfile> OnPressStartGame = new UnityEvent<TerrainGenerator.GenerationSettings, TerrainSettings, VolumeProfile>();
    public UnityEvent<VolumeProfile> OnUpdatePostProcessing = new UnityEvent<VolumeProfile>();


    public UnityEvent OnPressQuit = new UnityEvent();


    public GameObject PlayMenuParent;

    public GameObject InvalidSeedText;


    public TMP_Text VersionNumber;

    public TMP_Text CurrentMapText;



    public List<WorldSettings> GenerationSettings = new List<WorldSettings>();


    [Serializable]
    public class WorldSettings
    {
        public string Name;
        public TerrainSettings Setting;
        public GameObject Background;
        public VolumeProfile PostProcessing;
    }

    public int CurrentSetting = 0;


    private void Awake()
    {
        UpdateCurrentSettings();

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

        VersionNumber.text = $"v{Application.version} (Unity {Application.unityVersion})";
    }

    private void UpdateCurrentSettings()
    {
        for (int i = 0; i < GenerationSettings.Count; i++)
        {
            var setting = GenerationSettings[i];
            setting.Background.SetActive(i == CurrentSetting);
        }

        CurrentMapText.text = $"Map: {GenerationSettings[CurrentSetting].Name}";

        OnUpdatePostProcessing.Invoke(GenerationSettings[CurrentSetting].PostProcessing);
    }

    public void SelectNextSettings()
    {
        CurrentSetting++;

        if (CurrentSetting >= GenerationSettings.Count)
        {
            CurrentSetting = 0;
        }

        UpdateCurrentSettings();
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
            OnPressStartGame.Invoke(Settings, GenerationSettings[CurrentSetting].Setting, GenerationSettings[CurrentSetting].PostProcessing);
        }
        catch (System.Exception)
        {
        }
    }

    public void GenerateRandomSeed()
    {
        SeedInput.text = Noise.RandomSeed.ToString();
    }

    public void Reset()
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

    public void OpenCredits()
    {
        Application.OpenURL("https://github.com/SolomonBaarda/ProceduralGolf/");
    }
}
