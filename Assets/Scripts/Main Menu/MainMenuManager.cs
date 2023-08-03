using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MainMenuManager : MonoBehaviour, IManager
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    public UnityEvent<TerrainGenerator.GenerationSettings> OnPressStartGame = new UnityEvent<TerrainGenerator.GenerationSettings>();
    public UnityEvent OnPressQuit = new UnityEvent();


    public GameObject PlayMenuParent;

    public GameObject InvalidSeedText;

    private void Awake()
    {
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

    public void OpenBrokenVectorCredits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/12124");
    }

    public void OpenYuki2022Credits()
    {
        Application.OpenURL("https://assetstore.unity.com/publishers/57827");
    }

}
