using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public Vector2Int Position;
    public Bounds Bounds;

    [SerializeField] public Texture2D BiomeColourMap;
    [SerializeField] public Mesh MainMesh;
    public int MainMeshID; // Used for parallel mesh baking


    public int Width, Height;
    [SerializeField][HideInInspector] public Biome.Type[] Biomes;

    [SerializeField] public List<WorldObjectData> WorldObjects;


    public TerrainChunkData(Vector2Int position, Bounds bounds, Biome.Type[] biomes, int biomesWidth, int biomesHeight, Texture2D colourMap, Mesh main, List<WorldObjectData> worldObjects)
    {
        Position = position;
        Bounds = bounds;

        BiomeColourMap = colourMap;
        MainMesh = main;
        MainMeshID = main.GetInstanceID();

        Width = biomesWidth;
        Height = biomesHeight;
        Biomes = biomes;

        WorldObjects = worldObjects;
    }
}
