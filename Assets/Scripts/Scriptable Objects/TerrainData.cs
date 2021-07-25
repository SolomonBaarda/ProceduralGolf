using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainData : ScriptableObject
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<CourseData> Courses;

    public MapData MapData;

    public void SetData(int seed, List<TerrainChunkData> chunks, List<CourseData> courses, string terrainSettingsName, MapData mapData)
    {
        Seed = seed;
        Courses = courses;
        Chunks = chunks;
        TerrainSettingsName = terrainSettingsName;
        MapData = mapData;
    }


}
