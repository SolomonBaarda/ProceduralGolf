using System;
using UnityEngine;

[Serializable]
public class CourseData
{
    public Vector3 Start, Hole;

    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;


    public CourseData(Vector3 start, Vector3 finish)
    {
        Start = start;
        Hole = finish;
    }


    public bool BallWasPotted(int layerMask)
    {
        const float radius = 0.2f;
        return Physics.CheckSphere(Hole, radius, layerMask);
    }




}
