using System.Collections.Generic;

public class TerrainData
{
    public int Seed;
    public string TerrainSettingsName;

    public List<TerrainChunkData> Chunks;
    public List<CourseData> Courses;

    public HashSet<Biome.Type> InvalidBiomesForCurrentCourse;

    public TerrainData(int seed, List<TerrainChunkData> chunks, List<CourseData> courses, HashSet<Biome.Type> invalidBiomesForCurrentCourse, string terrainSettingsName)
    {
        Seed = seed;
        Courses = courses;
        Chunks = chunks;
        InvalidBiomesForCurrentCourse = invalidBiomesForCurrentCourse;
        TerrainSettingsName = terrainSettingsName;
    }
}
