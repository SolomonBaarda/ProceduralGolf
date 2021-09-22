using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public TerrainGenerator.GenerationSettings Settings = new TerrainGenerator.GenerationSettings();

    private void Start()
    {
        SceneManager.LoadSceneAsync(Scenes.GameSceneName, LoadSceneMode.Additive);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        GameManager.OnRequestStartGenerating.Invoke(Settings);
    }




}
