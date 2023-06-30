using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public Vector2Int Position;
    public Bounds Bounds;

    [SerializeField] public Mesh MainMesh;

    public int Width, Height;
    [SerializeField][HideInInspector] public Biome.Type[] Biomes;

    [SerializeField] public List<WorldObjectData> WorldObjects;


    public TerrainChunkData(Vector2Int position, Bounds bounds, Biome.Type[] biomes, int biomesWidth, int biomesHeight, Mesh main, List<WorldObjectData> worldObjects)
    {
        Position = position;
        Bounds = bounds;

        MainMesh = main;

        Width = biomesWidth;
        Height = biomesHeight;
        Biomes = biomes;

        WorldObjects = worldObjects;
    }
}
