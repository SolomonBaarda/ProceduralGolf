using UnityEngine;
using System.Collections.Generic;

public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    public float[] Heights;

    // Raw values
    public float[] BunkerHeights;
    public float[] LakeHeights;
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

    public void Normalise(float heightMin, float heightMax)
    {
        Noise.NormaliseNoise(ref Heights, heightMin, heightMax);
    }

}




