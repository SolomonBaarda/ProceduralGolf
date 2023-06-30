using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TerrainChunkData
{
    public Vector2Int Position;

    public List<Mesh> Meshes;

    public int Width, Height;
    [SerializeField][HideInInspector] public Biome.Type[] Biomes;

    [SerializeField] public List<WorldObjectData> WorldObjects;


    public TerrainChunkData(Vector2Int position, Biome.Type[] biomes, int biomesWidth, int biomesHeight, List<Mesh> meshes, List<WorldObjectData> worldObjects)
    {
        Position = position;

        Meshes = meshes;

        Width = biomesWidth;
        Height = biomesHeight;
        Biomes = biomes;

        WorldObjects = worldObjects;
    }
}
