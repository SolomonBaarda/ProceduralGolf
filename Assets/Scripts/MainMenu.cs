using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MainMenu : MonoBehaviour, IManager
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    public UnityEvent<TerrainGenerator.GenerationSettings> OnPressStartGame = new UnityEvent<TerrainGenerator.GenerationSettings>();
    public UnityEvent OnPressQuit = new UnityEvent();


    private void Start()
    {
        GenerateRandomSeed();
    }

    public void QuitGame()
    {
        OnPressQuit.Invoke();
    }

    public void StartGame()
    {
        try
        {
            Settings.Seed = int.Parse(SeedInput.text);
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
    }

    public void Clear()
    {

    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
