using System.Collections;
using System.Collections.Generic;
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


    public void UpdateMinimapForCourse(Vector3 holePosition)
    {
        Vector3 holePos = holePosition;
        holePos.y = MinimapIconsHeight;

        HoleMinimapIcon.position = holePos;
    }

    private void Update()
    {
        MinimapCamera.transform.position = GolfBall.position + CameraFollowOffset;

        Vector3 ballPos = GolfBall.position;
        ballPos.y = MinimapIconsHeight;
        GolfballMinimapIcon.transform.position = ballPos;
    }


    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {

    }
}
