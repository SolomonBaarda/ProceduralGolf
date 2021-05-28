using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<Green> Greens;
    public List<CourseData> GolfHoles;


    public void SetData(int seed, List<TerrainChunkData> chunks, List<Green> greens, List<CourseData> holes, string terrainSettingsName)
    {
        Seed = seed;
        Greens = greens;
        GolfHoles = holes;
        Chunks = chunks;
        TerrainSettingsName = terrainSettingsName;
    }


}
