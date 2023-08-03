using UnityEngine;
using TMPro;

public class LoadingScreenManager : MonoBehaviour, IManager
{
    public const string SceneName = "LoadingScreen";

    public Transform Parent;
    public TMP_Text Info;


    void Awake()
    {
        SetVisible(false);
    }

    public void Reset()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        Parent.gameObject.SetActive(visible);
    }

}
