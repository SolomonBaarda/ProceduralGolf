using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotPreview : MonoBehaviour
{
    [Header("Line Previews")]
    public LinePreview ShotPreviewMain;
    public LinePreview ShotPreviewMinimap;

    public Transform ShotPreviewTarget;
    public Transform ShotPreviewStart;

    public TextMesh ShotAngleText;
    public Transform ShotAnglePosition;

    public void UpdateShotPreview(string angleText, float angle, Vector3[] previewPositions, Quaternion rotation)
    {
        if(previewPositions.Length > 0)
        {
            // Update the shot angle text
            ShotAngleText.text = angleText;
            ShotAngleText.transform.localEulerAngles = new Vector3(0, 90, angle);
            ShotAnglePosition.SetPositionAndRotation(previewPositions[0], rotation);

            // Update the shot preview
            ShotPreviewMain.SetPoints(previewPositions);
            ShotPreviewMinimap.SetPoints(new Vector3[] { previewPositions[0] + Vector3.up * 10, previewPositions[previewPositions.Length - 1] + Vector3.up * 10 });

            // Update the start and end positions
            ShotPreviewStart.SetPositionAndRotation(previewPositions[0], rotation);
            ShotPreviewTarget.SetPositionAndRotation(previewPositions[previewPositions.Length - 1], rotation);
        }
        else
        {
            Debug.LogError("Shot preview cannot be updated as preview positions array has no elements");
        }

    }

}
