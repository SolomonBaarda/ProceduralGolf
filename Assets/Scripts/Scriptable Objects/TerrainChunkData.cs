using System;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public int X, Y;

    public Vector3 Centre;
    public Vector3 Size;

    [SerializeField] public Texture2D BiomeColourMap;
    [SerializeField] public Mesh MainMesh;


    public int Width, Height;
    [SerializeField] [HideInInspector] public Biome.Type[] BiomesFlat;



    public TerrainChunkData(int x, int y, Vector3 centre, Vector3 size, Biome.Type[,] biomes, Texture2D colourMap, Mesh main)
    {
        X = x;
        Y = y;
        Centre = centre;
        Size = size;

        BiomeColourMap = colourMap;
        MainMesh = main;

        Width = biomes.GetLength(0);
        Height = biomes.GetLength(1);

        BiomesFlat = Utils.Flatten(biomes);
    }
}
