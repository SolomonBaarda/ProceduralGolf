using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinimapManager : MonoBehaviour, IManager 
{
    public GolfBall GolfBall;

    [Header("Minimap rendering")]
    public Camera MinimapCamera;
    public Vector3 CameraFollowOffset = Vector3.zero;

    [Header("Minimap icons")]
    public float MinimapIconsHeight = 50.0f;
    public Transform GolfballMinimapIcon;
    public Transform HoleMinimapIcon;

    [Header("Path hint")]
    public LineRenderer PathStartToEnd;
    public float PathHintNumDashesPerWorldUnit = 0.02f;

    List<Vector2> fullCoursePath2D = new List<Vector2>();

    [Header("Shot preview")]
    public LineRenderer ShotPreview;
    public float ShotPreviewNumDashesPerWorldUnit = 0.02f;


    private void Awake()
    {
        GolfBall.OnRollingFinished += UpdateMinimapBeforeShot;
    }

    public void UpdateMinimapShotPreview(Vector3 estimatedLandingPoint, float ballAimingDirectionAngle)
    {
        // Rotate the ball sprite
        Vector3 ballRotation = GolfballMinimapIcon.eulerAngles;
        ballRotation.y = ballAimingDirectionAngle;
        GolfballMinimapIcon.eulerAngles = ballRotation;

        // Update the shot preview
        ShotPreview.positionCount = 2;
        ShotPreview.SetPositions(new Vector3[]
        {
            new Vector3(GolfBall.transform.position.x, MinimapIconsHeight, GolfBall.transform.position.z ),
            new Vector3(estimatedLandingPoint.x, MinimapIconsHeight, estimatedLandingPoint.z)
        });

        float pathLength = 
            (
                new Vector2(estimatedLandingPoint.x, estimatedLandingPoint.z) - 
                new Vector2(GolfBall.transform.position.x, GolfBall.transform.position.z)
            ).magnitude;

        Material dashedPathMat = ShotPreview.material;
        float numDashes = pathLength * ShotPreviewNumDashesPerWorldUnit;
        dashedPathMat.SetFloat("_NumberOfDashes", numDashes);
    }

    private void UpdateMinimapBeforeShot()
    {
        // Move the camera
        MinimapCamera.transform.position = GolfBall.transform.position + CameraFollowOffset;


        // Update the ball position
        Vector3 ballPos = GolfBall.transform.position;
        ballPos.y = MinimapIconsHeight;
        GolfballMinimapIcon.transform.position = ballPos;


        // Calculate if the flag is visible on the minimap
        Vector3 diff = HoleMinimapIcon.position - GolfBall.transform.position;
        Vector2 distanceToHole = new Vector2(diff.x, diff.z);
        bool isHoleVisibleOnMinimap = distanceToHole.sqrMagnitude <= MinimapCamera.orthographicSize * MinimapCamera.orthographicSize;

        // Make the direction hint visible if the hole is off the screen
        PathStartToEnd.enabled = !isHoleVisibleOnMinimap;


        // Update the path preview if we need to
        if (!isHoleVisibleOnMinimap)
        {
            int closestPositionIndex = 0;
            Vector2 currentBallPos2D = new Vector2(GolfBall.transform.position.x, GolfBall.transform.position.z);
            float distanceToClosestPosition = (fullCoursePath2D[0] - currentBallPos2D).sqrMagnitude;

            // Find the closest position
            for (int i = 1; i < fullCoursePath2D.Count; i++)
            {
                float newDistance = (fullCoursePath2D[i] - currentBallPos2D).sqrMagnitude;

                if (newDistance < distanceToClosestPosition)
                {
                    distanceToClosestPosition = newDistance;
                    closestPositionIndex = i;
                }
            }

            // Increment this value as we will use the current ball position instead
            closestPositionIndex++;

            // Construct the new list of points with the initial ones removed
            List<Vector2> pathFromCurrentPos2D = new List<Vector2>()
            {
                // Add the ball current position as the closest point
                currentBallPos2D,
            };
            // Add the remaining points to the hole
            int numElementsToAdd = fullCoursePath2D.Count - closestPositionIndex;
            pathFromCurrentPos2D.AddRange(fullCoursePath2D.GetRange(closestPositionIndex, numElementsToAdd));

            // Update path from start to end
            PathStartToEnd.positionCount = pathFromCurrentPos2D.Count;
            PathStartToEnd.SetPositions(pathFromCurrentPos2D.Select(x => new Vector3(x.x, MinimapIconsHeight, x.y)).ToArray());

            // Calculate the path length
            float pathLength = Utils.CalculatePathLengthWorldUnits(pathFromCurrentPos2D);

            // Update the dashed line material to show the correct number of dashes for the distance
            Material dashedPathMat = PathStartToEnd.material;
            float numDashes = pathLength * PathHintNumDashesPerWorldUnit;
            dashedPathMat.SetFloat("_NumberOfDashes", numDashes);
        }
    }

    public void UpdateMinimapForCourse(Vector3 holePosition, List<Vector3> pathStartToEnd)
    {
        // Update flag icon
        Vector3 holePos = holePosition;
        holePos.y = MinimapIconsHeight;
        HoleMinimapIcon.position = holePos;

        fullCoursePath2D = pathStartToEnd.Select(x => new Vector2(x.x, x.z)).ToList();
        UpdateMinimapBeforeShot();
    }


    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {

    }
}
