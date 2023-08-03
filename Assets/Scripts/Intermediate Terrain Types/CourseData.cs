using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CourseData
{
    public Vector3 Start, Hole;
    public List<Vector3> PathStartToEnd;

    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;

    public Color32 Colour;

    public CourseData(Vector3 start, Vector3 finish, List<Vector3> path, Color32 colour)
    {
        Start = start;
        Hole = finish;
        PathStartToEnd = path;
        Colour = colour;
    }


    public bool BallWasPotted(int layerMask)
    {
        const float radius = 0.2f;
        return Physics.CheckSphere(Hole, radius, layerMask);
    }




}
