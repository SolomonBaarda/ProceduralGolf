using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public const string SceneName = "LoadingScreen";

    public static LoadingScreen Instance;

    public Transform Parent;
    public TMP_Text Info; 


    void Awake()
    {
        Instance = this;
    }

    public static void Active(bool visible)
    {
        Instance.Parent.gameObject.SetActive(visible);
    }
}
