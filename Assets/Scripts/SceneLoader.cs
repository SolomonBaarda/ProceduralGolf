using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public const string GameSceneName = "Game", HUDSceneName = "HUD", LoadingScreenSceneName = "LoadingScene", MainMenuSceneName = "MainMenu";


    public GameManager GameManager { get; private set; }
    public HUD HUD { get; set; }
    public MainMenu MainMenu { get; private set; }


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(WaitForScenesToLoad());
    }

    private IEnumerator WaitForScenesToLoad()
    {
        LoadSceneParameters p = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.None);

        AsyncOperation game = SceneManager.LoadSceneAsync(GameSceneName, p);
        AsyncOperation hud = SceneManager.LoadSceneAsync(HUDSceneName, p);

        while (!game.isDone || !hud.isDone)
        {
            yield return null;
        }

        yield return null;

        MainMenu = FindObjectOfType<MainMenu>(true);
        GameManager = FindObjectOfType<GameManager>(true);
        HUD = FindObjectOfType<HUD>(true);

        GameManager.SetHUD(HUD);

        GameManager.OnGameBegin.AddListener(() => MainMenu.SetVisible(false));

        MainMenu.OnPressStartGame.AddListener(StartGame);
        MainMenu.OnPressQuit.AddListener(QuitGame);

        HUD.OnQuitToMenuPressed.AddListener(QuitToMenu);
        HUD.OnRestartPressed.AddListener(RestartGame);

        MainMenu.SetLoading(false);
        MainMenu.SetVisible(true);
        GameManager.SetVisible(false);
        HUD.SetVisible(false);
    }

    private void StartGame(TerrainGenerator.GenerationSettings settings)
    {
        MainMenu.SetLoading(true);
        GameManager.StartGeneration(settings, false);
    }

    private void QuitToMenu()
    {
        MainMenu.SetVisible(true);
        MainMenu.SetLoading(false);

        GameManager.SetVisible(false);
        HUD.SetVisible(false);
    }

    private void RestartGame()
    {
        GameManager.RestartGame();
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
