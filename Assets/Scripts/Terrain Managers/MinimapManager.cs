using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinimapManager : MonoBehaviour, IManager 
{
    public Transform GolfBall;

    [Header("Minimap rendering")]
    public Camera MinimapCamera;
    public Vector3 CameraFollowOffset = Vector3.zero;

    [Header("Minimap icons")]
    public float MinimapIconsHeight = 50.0f;
    public Transform GolfballMinimapIcon;
    public Transform HoleMinimapIcon;

    [Space]
    public LineRenderer PathStartToEnd;

    public bool DisplayPathToHole = false;

    public void UpdateMinimapForCourse(Vector3 holePosition, List<Vector3> pathStartToEnd)
    {
        // Update flag icon
        Vector3 holePos = holePosition;
        holePos.y = MinimapIconsHeight;

        HoleMinimapIcon.position = holePos;

        // Update path from start to end
        PathStartToEnd.positionCount = pathStartToEnd.Count;
        PathStartToEnd.SetPositions(pathStartToEnd.Select(x => new Vector3(x.x, MinimapIconsHeight, x.z)).ToArray());
    }

    private void Update()
    {
        // Update minimap background
        MinimapCamera.transform.position = GolfBall.position + CameraFollowOffset;

        // Update ball position
        Vector3 ballPos = GolfBall.position;
        ballPos.y = MinimapIconsHeight;
        GolfballMinimapIcon.transform.position = ballPos;

        // Calculate if the flag is visible on the minimap
        Vector3 diff = HoleMinimapIcon.position - GolfBall.transform.position;
        Vector2 distanceToHole = new Vector2(diff.x, diff.z);
        bool isHoleVisibleOnMinimap = distanceToHole.sqrMagnitude <= MinimapCamera.orthographicSize * MinimapCamera.orthographicSize;

        // Make the direction hint visible if the hole is off the screen
        PathStartToEnd.enabled = !isHoleVisibleOnMinimap;
    }


    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {

    }
}
