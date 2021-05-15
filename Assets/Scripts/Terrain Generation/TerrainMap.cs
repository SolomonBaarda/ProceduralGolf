using System.Collections.Generic;
using UnityEngine;

public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    /// <summary>
    /// The final height values for this terrain map
    /// </summary>
    public float[] Heights;

    /// <summary>
    /// List of height layers to combine 
    /// </summary>
    public List<Layer> Layers;

    public bool[] TreeMask;
    public bool[] RockMask;

    public Biome.Type[] Biomes;

    public Dictionary<Biome.Decoration, List<Vector3>> Decoration = new Dictionary<Biome.Decoration, List<Vector3>>();

    public TerrainMap(Vector2Int chunk, int width, int height, Bounds bounds)
    {
        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;
    }

    public void NormaliseHeights(float min, float max)
    {
        //Debug.Log("final layer with min: " + min + " max: " + max);
        Noise.NormaliseNoise(ref Heights, min, max);
    }

    public void NormaliseLayers(List<(float, float)> minMax)
    {
        for (int i = 0; i < minMax.Count; i++)
        {
            //Debug.Log("layer " + i+" with min: " + minMax[i].Item1 +" max: " + minMax[i].Item2);
            Noise.NormaliseNoise(ref Layers[i].Noise, minMax[i].Item1, minMax[i].Item2);
        }
    }

    public class Layer
    {
        public float[] Noise;
        public float Min = float.MaxValue, Max = float.MinValue;
        public Biome.Type Biome;
    }


    public class WorldObjectData
    {
        public Biome.Decoration Decoration;
        public List<Vector3> WorldPositions;


    }



}




