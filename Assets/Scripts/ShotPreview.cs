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

    public TextMesh ShotAnglePreview;

    public void SetShotPreviewPoints(string angleText, float angle, Vector3[] positions, Quaternion rotation)
    {
        // Update the shot angle text
        ShotAnglePreview.text = angleText;


        // TODO fix angle text
        // add camera angles
        // check for other bounces in preview?
        // Fix minimap to use new cinemachine?
        // Add postprocessing to cinemachine


        // TODO Disable objects on scene loading

        ShotAnglePreview.transform.localEulerAngles = new Vector3(0, 90, angle);

        // Update the shot preview
        ShotPreviewMain.SetPoints(positions);
        ShotPreviewMinimap.SetPoints(new Vector3[] { positions[0] + Vector3.up * 10, positions[positions.Length - 1] + Vector3.up * 10 });

        // Update the start and end positions
        ShotPreviewStart.SetPositionAndRotation(positions[0], rotation);
        ShotPreviewTarget.SetPositionAndRotation(positions[positions.Length - 1], rotation);
    }

}
