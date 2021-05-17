using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WorldObjectData
{
    public GameObject Prefab;
    /// <summary>
    /// Position and rotation
    /// </summary>
    public List<(Vector3, Vector3)> WorldPositions;
}
