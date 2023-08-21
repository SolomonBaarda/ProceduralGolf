using System.Collections.Generic;
using UnityEngine;

public class TerrainData
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<CourseData> Courses;

    public HashSet<Biome.Type> InvalidBiomesForCurrentCourse;
    public Color32 BackgroundColour;

    public TerrainData(int seed, List<TerrainChunkData> chunks, List<CourseData> courses, HashSet<Biome.Type> invalidBiomesForCurrentCourse, Color32 backgroundColour, string terrainSettingsName)
    {
        Seed = seed;
        Courses = courses;
        Chunks = chunks;
        InvalidBiomesForCurrentCourse = invalidBiomesForCurrentCourse;
        BackgroundColour = backgroundColour;
        TerrainSettingsName = terrainSettingsName;
    }
}
