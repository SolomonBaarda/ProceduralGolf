using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;
    [SerializeField] public List<Hole> GolfHoles;
    [SerializeField] public List<TerrainChunkData> Chunks;


    public void SetData(int seed, List<Hole> holes, List<TerrainChunkData> chunks)
    {
        Seed = seed;
        GolfHoles = holes;
        Chunks = chunks;
    }


}
