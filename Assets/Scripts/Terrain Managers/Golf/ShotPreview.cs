using System.Linq;
using UnityEngine;

public class ShotPreview : MonoBehaviour
{
    [Header("Line Previews")]
    public LineRenderer ShotPreviewMain;
    public float ShotPreviewNumDashesPerWorldUnit = 0.02f;
    public float ShotPreviewDashesSpeed = 1.0f;

    [Header("Key positions")]
    public Transform AimingPosition;
    public Transform StartingPosition;
    public Transform ShotPeakPosition;
    public Transform ShotCentrePosition;

    [Header("Graphics")]
    public TextMesh ShotAngleText;
    public Transform ShotAnglePosition;

    public void UpdateShotPreview(string angleText, float angle, Vector3[] previewPositions, Quaternion rotation, out Vector3 peakPos, out Vector3 minPos)
    {
        // Update the shot angle text
        ShotAngleText.text = angleText;
        ShotAngleText.transform.localEulerAngles = new Vector3(0, 90, angle);
        ShotAnglePosition.SetPositionAndRotation(previewPositions[0], rotation);

        // Update the shot preview
        ShotPreviewMain.positionCount = previewPositions.Length;
        ShotPreviewMain.SetPositions(previewPositions);

        float length = Utils.CalculatePathLengthWorldUnits(previewPositions);
        Material dashedPathMat = ShotPreviewMain.material;
        dashedPathMat.SetFloat("_NumberOfDashes", length * ShotPreviewNumDashesPerWorldUnit);
        dashedPathMat.SetFloat("_DashMovementSpeed", ShotPreviewDashesSpeed);


        // Update the start and end positions
        StartingPosition.SetPositionAndRotation(previewPositions[0], rotation);
        AimingPosition.SetPositionAndRotation(previewPositions[previewPositions.Length - 1], rotation);

        var sortedByY = previewPositions.OrderByDescending(x => x.y);
        peakPos = sortedByY.First();
        minPos = sortedByY.Last();

        ShotPeakPosition.SetPositionAndRotation(peakPos, rotation);

        float halfHeight = (peakPos.y - minPos.y) / 2;
        ShotCentrePosition.SetPositionAndRotation(peakPos - new Vector3(0, halfHeight, 0), rotation);
    }

}
