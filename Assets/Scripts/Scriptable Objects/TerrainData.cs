using System.Collections.Generic;
using UnityEngine;


[SerializeField]
public class TerrainData : ScriptableObject
{
    public int Seed;
    [SerializeField]
    public HashSet<Hole> GolfHoles;
    [SerializeField]
    public HashSet<TerrainChunkData> Chunks;


    public void SetData(int seed, HashSet<Hole> holes, HashSet<TerrainChunkData> chunks)
    {
        Seed = seed;
        GolfHoles = holes;
        Chunks = chunks;
    }


}
