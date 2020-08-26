using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;

    public List<TerrainChunkData> Chunks;
    public List<HoleData> GolfHoles;
    public List<WorldObjectData> WorldObjects;


    public void SetData(int seed, List<TerrainChunkData> chunks, List<HoleData> holes, List<WorldObjectData> worldObjects)
    {
        Seed = seed;
        GolfHoles = holes;
        Chunks = chunks;
        WorldObjects = worldObjects;
    }


}
