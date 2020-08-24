using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;

    public List<HoleData> GolfHoles;
    public List<TerrainChunkData> Chunks;


    public void SetData(int seed, List<HoleData> holes, List<TerrainChunkData> chunks)
    {
        Seed = seed;
        GolfHoles = holes;
        Chunks = chunks;
    }


}
