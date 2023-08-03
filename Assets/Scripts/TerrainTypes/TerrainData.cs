using System.Collections.Generic;
using System;

public class TerrainData
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<CourseData> Courses;

    public TerrainData(int seed, List<TerrainChunkData> chunks, List<CourseData> courses, string terrainSettingsName)
    {
        Seed = seed;
        Courses = courses;
        Chunks = chunks;
        TerrainSettingsName = terrainSettingsName;
    }
}
