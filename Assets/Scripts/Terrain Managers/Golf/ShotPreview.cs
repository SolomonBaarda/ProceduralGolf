using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShotPreview : MonoBehaviour
{
    [Header("Line Previews")]
    public LinePreview ShotPreviewMain;

    [Header("Key positions")]
    public Transform AimingPosition;
    public Transform StartingPosition;
    public Transform ShotPeakPosition;

    [Header("Graphics")]
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

            // Update the start and end positions
            StartingPosition.SetPositionAndRotation(previewPositions[0], rotation);
            AimingPosition.SetPositionAndRotation(previewPositions[previewPositions.Length - 1], rotation);
            ShotPeakPosition.SetPositionAndRotation(previewPositions.OrderByDescending(x => x.y).First(), rotation);
        }
        else
        {
            Debug.LogError("Shot preview cannot be updated as preview positions array has no elements");
        }

    }

}
