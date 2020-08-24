using System;
using UnityEngine;

[Serializable]
public class HoleData
{
    public Vector3 Centre;

    public const int NotAssignedHoleNumber = -1;
    public int Number = NotAssignedHoleNumber;


    public HoleData(Vector3 centre)
    {
        Centre = centre;
    }


}
