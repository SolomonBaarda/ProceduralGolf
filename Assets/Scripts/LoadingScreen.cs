using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour, IManager
{
    public const string SceneName = "LoadingScreen";

    public static LoadingScreen Instance;

    public Transform Parent;
    public TMP_Text Info;


    void Awake()
    {
        Instance = this;
        SetVisible(false);
    }

    public void Clear()
    {

    }

    public void SetVisible(bool visible)
    {
        Instance.Parent.gameObject.SetActive(visible);
    }

}
