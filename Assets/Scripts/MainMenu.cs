using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    public TMP_InputField SeedInput;


    private void Start()
    {
        SceneManager.LoadSceneAsync(Scenes.GameSceneName, LoadSceneMode.Additive);

        GenerateRandomSeed();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        try
        {
            Settings.Seed = int.Parse(SeedInput.text);
            GameManager.OnRequestStartGenerating.Invoke(Settings);
        }
        catch (System.Exception e)
        {
        }
    }


    public void GenerateRandomSeed()
    {
        SeedInput.text = Noise.RandomSeed.ToString();
    }

}
