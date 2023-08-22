using UnityEngine;
using TMPro;

public class LoadingScreenManager : MonoBehaviour, IManager
{
    public const string SceneName = "LoadingScreen";

    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

}
