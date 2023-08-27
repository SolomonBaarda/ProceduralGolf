using System.Collections.Generic;
using UnityEngine;

public class TerrainMap
{
    public int Width, Height;

    /// <summary>
    /// The final height values for this terrain map
    /// </summary>
    public float[] Heights;
    public Biome.Type[] Biomes;
    public bool[] Greens;

    public bool[] Holes;


    /// <summary>
    /// List of height layers to combine 
    /// </summary>
    public List<Layer> Layers = new List<Layer>();
    /// <summary>
    /// List of world objects in the map
    /// </summary>
    public List<WorldObjectData> WorldObjects = new List<WorldObjectData>();

    public TerrainMap(int width, int height)
    {
        Width = width;
        Height = height;

        int size = width * height;
        Heights = new float[size];
        Biomes = new Biome.Type[size];
        Greens = new bool[size];
        Holes = new bool[size];
    }

    public class Layer
    {
        public float[] Noise;
        public Biome.Type Biome;

        public Layer(float[] noise, Biome.Type biome)
        {
            Noise = noise;
            Biome = biome;
        }
    }

    public class WorldObjectData
    {
        public GameObject Prefab;
        public Vector3 LocalPosition;
        public Vector3 Rotation;
        public int ClosestIndexX, ClosestIndexY;
    }
}
