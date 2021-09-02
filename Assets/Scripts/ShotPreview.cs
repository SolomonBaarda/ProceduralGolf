using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotPreview : MonoBehaviour
{
    [Header("Line Previews")]
    public LinePreview ShotPreviewMain;
    public LinePreview ShotPreviewMinimap;

    public Transform ShotPreviewTarget;

    public TextMesh ShotAnglePreview;

    public void SetPoints(Vector3[] positions)
    {
        ShotPreviewMain.SetPoints(positions);
        ShotPreviewMinimap.SetPoints(new Vector3[] { positions[0] + Vector3.up * 10, positions[positions.Length - 1] + Vector3.up * 10 });
        ShotPreviewTarget.position = positions[positions.Length - 1];
    }

    public void SetAnglePreview(Vector3 localEuler, string text)
    {
        ShotAnglePreview.transform.localEulerAngles = localEuler;
        ShotAnglePreview.text = text;
    }

}
