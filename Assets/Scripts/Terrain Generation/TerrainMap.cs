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
    public Biome.Type[] Biomes;
    public bool[] Greens;

    /// <summary>
    /// List of height layers to combine 
    /// </summary>
    public List<Layer> Layers = new List<Layer>();
    /// <summary>
    /// List of world objects in the chunk
    /// </summary>
    public List<WorldObjectData> WorldObjects = new List<WorldObjectData>();


    public TerrainMap(Vector2Int chunk, int width, int height, Bounds bounds)
    {
        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;

        int size = width * height;
        Heights = new float[size];
        Biomes = new Biome.Type[size];
        Greens = new bool[size];
    }

    public void NormaliseHeights(float min, float max)
    {
        //Logger.Log("final layer with min: " + min + " max: " + max);
        Noise.NormaliseNoise(ref Heights, min, max);
    }

    public void NormaliseLayers(List<(float, float)> minMax)
    {
        for (int i = 0; i < minMax.Count; i++)
        {
            //Logger.Log("layer " + i+" with min: " + minMax[i].Item1 +" max: " + minMax[i].Item2);

            // Ensure noise is not null as this layer could be sharing noise with another layer
            if(Layers[i].Noise != null)
            {
                Noise.NormaliseNoise(ref Layers[i].Noise, minMax[i].Item1, minMax[i].Item2);
            }
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
        public GameObject Prefab;
        public Vector3 LocalPosition;
        public Vector3 Rotation;
        public int ClosestIndexX, ClosestIndexY;
    }

    public static bool IsSharedPositionOnBorder(Vector2Int a, int aX, int aY, Vector2Int b, int bX, int bY, int width, int height)
    {
        static bool IsEqual(int aChunkX, int aChunkY, int bChunkX, int bChunkY, int aIndex, int aEdge, int bIndex, int bEdge, int aOtherIndex, int bOtherIndex)
        {
            return aChunkX == bChunkX && aChunkY == bChunkY && aIndex == aEdge && bIndex == bEdge && aOtherIndex == bOtherIndex;
        }

        return
            // Left
            IsEqual(a.x - 1, a.y, b.x, b.y, aX, 0, bX, width - 1, aY, bY) ||
            // Right
            IsEqual(a.x + 1, a.y, b.x, b.y, aX, width - 1, bX, 0, aY, bY) ||
            // Up
            IsEqual(a.x, a.y - 1, b.x, b.y, aY, 0, bY, height - 1, aX, bX) ||
            // Down
            IsEqual(a.x, a.y + 1, b.x, b.y, aY, height - 1, bY, 0, aX, bX);
    }

}




