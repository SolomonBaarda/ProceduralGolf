using System;
using UnityEngine;

[Serializable]
public class CourseData
{
    public Vector3 Start, Hole;

    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;

    public Color32 Colour;

    public CourseData(Vector3 start, Vector3 finish, Color32 colour)
    {
        Start = start;
        Hole = finish;
        Colour = colour;
    }


    public bool BallWasPotted(int layerMask)
    {
        const float radius = 0.2f;
        return Physics.CheckSphere(Hole, radius, layerMask);
    }




}
