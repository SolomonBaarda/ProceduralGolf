using System;
using UnityEngine;

[Serializable]
public class CourseData
{
    public Vector3 Start, Midpoint, Hole;

    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;

    public Color32 Colour;

    public CourseData(Vector3 start, Vector3 finish, Vector3 approxMidpoint, Color32 colour)
    {
        Start = start;
        Hole = finish;
        Midpoint = approxMidpoint;
        Colour = colour;
    }


    public bool BallWasPotted(int layerMask)
    {
        const float radius = 0.2f;
        return Physics.CheckSphere(Hole, radius, layerMask);
    }




}
