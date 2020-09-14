using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public int X, Y;

    public Vector3 Centre;
    public Vector3 BoundsSize { get { Vector3 size = MainMesh.bounds.size; size.y = 0; return size; } }

    [SerializeField] public Texture2D BiomeColourMap;
    [SerializeField] public Mesh MainMesh;


    public int Width, Height;
    [SerializeField] [HideInInspector] public Biome.Type[] BiomesFlat;

    [SerializeField] public List<WorldObjectData> WorldObjects;





    public TerrainChunkData(int x, int y, Vector3 centre, Biome.Type[,] biomes,
        Texture2D colourMap, Mesh main, List<WorldObjectData> worldObjects)
    {
        X = x;
        Y = y;
        Centre = centre;

        BiomeColourMap = colourMap;
        MainMesh = main;

        Width = biomes.GetLength(0);
        Height = biomes.GetLength(1);

        BiomesFlat = Utils.Flatten(biomes);

        WorldObjects = worldObjects;
    }
}
