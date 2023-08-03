using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public RawImage Graphic;
    public Transform Following;

    void Update()
    {
        if (Following != null && Graphic != null)
        {
            Graphic.uvRect = new Rect(Following.localEulerAngles.y / 360f, 0, 1, 1);
        }
    }
}

