using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static TerrainGenerator;

public class MainMenuManager : MonoBehaviour, IManager
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    public UnityEvent<TerrainGenerator.GenerationSettings, TerrainSettings> OnPressStartGame = new UnityEvent<TerrainGenerator.GenerationSettings, TerrainSettings>();
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
        for(int i = 0; i < GenerationSettings.Count; i++ )
        {
            var setting = GenerationSettings[i];
            setting.Background.SetActive(i == CurrentSetting);
        }

        CurrentMapText.text = $"Map: {GenerationSettings[CurrentSetting].Name}";
    }

    public void SelectNextSettings()
    {
        CurrentSetting++;

        if(CurrentSetting >= GenerationSettings.Count)
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
            OnPressStartGame.Invoke(Settings, GenerationSettings[CurrentSetting].Setting);
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

    // Trees
    public void OpenBrokenVectorCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/12124");
    }

    // Skybox
    public void OpenYuki2022Credits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/57827");
    }

    // Icons
    public void OpenJuicyFishCredits()
    {
        Application.OpenURL("https://www.flaticon.com/authors/juicy-fish");
    }

    // Flagpole
    public void OpenProrookie123Credits()
    {
        Application.OpenURL("https://sketchfab.com/prorookie123");
    }

    // Loading icons
    public void OpenInfimaGamesCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/13489");
    }

    // Loading icons
    public void OpenSyntyStudiosCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/5217");
    }

    // Toon shader
    public void OpenDELTationCredits()
    {
        Application.OpenURL("https://github.com/Delt06/urp-toon-shader");
    }

    // Water shader
    public void OpenIgniteCodersCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/53014");
    }

    // Desert models
    public void Open23SpaceRobotsandCountingCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/21779");
    }

}
