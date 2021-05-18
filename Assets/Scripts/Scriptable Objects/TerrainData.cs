using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<FloodFillBiome> Greens;
    public List<HoleData> GolfHoles;


    public void SetData(int seed, List<TerrainChunkData> chunks, List<FloodFillBiome> greens, List<HoleData> holes, string terrainSettingsName)
    {
        Seed = seed;
        Greens = greens;
        GolfHoles = holes;
        Chunks = chunks;
        TerrainSettingsName = terrainSettingsName;
    }


}
