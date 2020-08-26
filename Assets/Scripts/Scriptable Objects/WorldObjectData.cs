using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WorldObjectData
{
    public GameObject Prefab;
    public List<Vector3> WorldPositions;


    public WorldObjectData(GameObject prefab, List<Vector3> positions)
    {
        Prefab = prefab;
        WorldPositions = positions;
    }
}
