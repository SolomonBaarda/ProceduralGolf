using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public const string SceneName = "LoadingScreen";

    public static LoadingScreen Instance;

    public Transform Parent;


    void Awake()
    {
        Instance = FindObjectOfType<LoadingScreen>();
    }


    public static void Active(bool visible)
    {
        Instance.Parent.gameObject.SetActive(visible);
    }


}
