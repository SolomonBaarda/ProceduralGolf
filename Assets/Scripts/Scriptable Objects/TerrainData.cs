using System.Collections.Generic;
using UnityEngine;


[SerializeField]
public class TerrainData : ScriptableObject
{
    public int Seed;
    public HashSet<Hole> GolfHoles;
    public HashSet<TerrainChunkDataStorage> Chunks;


    public void SetData(int seed, HashSet<Hole> holes, HashSet<TerrainChunkDataStorage> chunks)
    {
        Seed = seed;
        GolfHoles = holes;
        Chunks = chunks;
    }


}
