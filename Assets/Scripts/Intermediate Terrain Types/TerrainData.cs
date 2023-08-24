using System.Collections.Generic;
using UnityEngine;

public class TerrainData
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<CourseData> Courses;

    public bool DoWater;
    public Mesh WaterMesh;
    public float WaterMeshHeight;
    public int WaterMeshTiling;

    public HashSet<Biome.Type> InvalidBiomesForCurrentCourse;
    public Color32 BackgroundColour;

    public TerrainData(int seed, List<TerrainChunkData> chunks, List<CourseData> courses, bool doWater, Mesh waterMesh, float waterMeshHeight, int waterMeshTiling,
        HashSet<Biome.Type> invalidBiomesForCurrentCourse, Color32 backgroundColour, string terrainSettingsName)
    {
        Seed = seed;
        Courses = courses;
        Chunks = chunks;
        DoWater = doWater;
        WaterMesh = waterMesh;
        WaterMeshHeight = waterMeshHeight;
        WaterMeshTiling = waterMeshTiling;
        InvalidBiomesForCurrentCourse = invalidBiomesForCurrentCourse;
        BackgroundColour = backgroundColour;
        TerrainSettingsName = terrainSettingsName;
    }
}
