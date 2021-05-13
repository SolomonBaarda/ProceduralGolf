using UnityEngine;
using System.Collections.Generic;

public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    public float[] Heights;
    public float HeightsMin, HeightsMax;

    // Raw values
    public float[] BunkerHeights;
    public float BunkerHeightMin, BunkerHeightMax;
    public float[] LakeHeights;
    public float LakeHeightMin, LakeHeightMax;
    public bool[] TreeMask;
    public bool[] RockMask;

    public Biome.Type[] Biomes;
    public Biome.Decoration[] Decoration;

    public TerrainMap(Vector2Int chunk, int width, int height, Bounds bounds)
    {
        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;
    }

    public void Normalise(float heightMin, float heightMax, float lakeMin, float lakeMax, float bunkerMin, float bunkerMax)
    {
        Noise.NormaliseNoise(ref Heights, heightMin, heightMax);
        Noise.NormaliseNoise(ref LakeHeights, lakeMin, lakeMax); 
        Noise.NormaliseNoise(ref BunkerHeights, bunkerMin, bunkerMax);
    }

}




